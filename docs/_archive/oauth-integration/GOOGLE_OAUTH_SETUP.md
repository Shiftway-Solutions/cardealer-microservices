# 🔵 Google OAuth Setup - Guía Completa

Esta guía detalla el proceso paso a paso para configurar Google OAuth en la plataforma OKLA.

## 📋 Tabla de Contenidos

1. [Requisitos Previos](#requisitos-previos)
2. [Paso 1: Crear Proyecto en Google Cloud](#paso-1-crear-proyecto-en-google-cloud)
3. [Paso 2: Configurar OAuth Consent Screen](#paso-2-configurar-oauth-consent-screen)
4. [Paso 3: Crear Credenciales OAuth](#paso-3-crear-credenciales-oauth)
5. [Paso 4: Configurar Backend](#paso-4-configurar-backend)
6. [Paso 5: Configurar Frontend](#paso-5-configurar-frontend)
7. [Paso 6: Probar la Integración](#paso-6-probar-la-integración)
8. [Configuración de Producción](#configuración-de-producción)

---

## Requisitos Previos

- ✅ Cuenta de Google (personal o Google Workspace)
- ✅ Proyecto OKLA funcionando localmente con Docker
- ✅ Acceso a [Google Cloud Console](https://console.cloud.google.com)

---

## Paso 1: Crear Proyecto en Google Cloud

### 1.1 Acceder a Google Cloud Console

1. Ve a [console.cloud.google.com](https://console.cloud.google.com)
2. Inicia sesión con tu cuenta de Google

### 1.2 Crear Nuevo Proyecto

1. Haz clic en el selector de proyectos (arriba a la izquierda)
2. Clic en **"New Project"**
3. Configura:
   - **Project name:** `okla-auth` (o el nombre que prefieras)
   - **Organization:** (opcional)
   - **Location:** (opcional)
4. Clic en **"Create"**

### 1.3 Seleccionar el Proyecto

1. Espera a que se cree el proyecto
2. Selecciónalo desde el selector de proyectos

---

## Paso 2: Configurar OAuth Consent Screen

### 2.1 Navegar a OAuth Consent Screen

1. En el menú lateral: **APIs & Services** → **OAuth consent screen**
2. O ve directamente a: https://console.cloud.google.com/apis/credentials/consent

### 2.2 Seleccionar Tipo de Usuario

| Tipo         | Descripción                                       | Usar para                  |
| ------------ | ------------------------------------------------- | -------------------------- |
| **Internal** | Solo usuarios de tu organización Google Workspace | Apps corporativas internas |
| **External** | Cualquier usuario con cuenta de Google            | Apps públicas como OKLA    |

Para OKLA, selecciona **External** y clic en **"Create"**.

### 2.3 Configurar App Information

**Página 1 - OAuth consent screen:**

| Campo                         | Valor                        | Ejemplo                       |
| ----------------------------- | ---------------------------- | ----------------------------- |
| App name                      | Nombre de tu app             | `OKLA`                        |
| User support email            | Email de soporte             | `support@okla.com.do`         |
| App logo                      | (Opcional) Logo de 120x120px |                               |
| App domain - Homepage         | URL de tu sitio              | `https://okla.com.do`         |
| App domain - Privacy policy   | URL de privacidad            | `https://okla.com.do/privacy` |
| App domain - Terms of service | URL de términos              | `https://okla.com.do/terms`   |
| Authorized domains            | Dominios autorizados         | `okla.com.do`                 |
| Developer contact             | Tu email                     | `developer@okla.com.do`       |

Clic en **"Save and Continue"**.

### 2.4 Configurar Scopes

**Página 2 - Scopes:**

1. Clic en **"Add or Remove Scopes"**
2. Selecciona los scopes necesarios:

| Scope                       | Descripción           | Requerido |
| --------------------------- | --------------------- | --------- |
| `.../auth/userinfo.email`   | Ver email del usuario | ✅ Sí     |
| `.../auth/userinfo.profile` | Ver perfil básico     | ✅ Sí     |
| `openid`                    | OpenID Connect        | ✅ Sí     |

3. Clic en **"Update"**
4. Clic en **"Save and Continue"**

### 2.5 Agregar Test Users (Modo Testing)

**Página 3 - Test users:**

Mientras la app está en modo "Testing", solo los usuarios listados aquí pueden autenticarse.

1. Clic en **"Add Users"**
2. Agrega emails de prueba:
   ```
   tu-email@gmail.com
   otro-tester@gmail.com
   ```
3. Clic en **"Save and Continue"**

### 2.6 Revisar y Confirmar

**Página 4 - Summary:**

1. Revisa toda la configuración
2. Clic en **"Back to Dashboard"**

---

## Paso 3: Crear Credenciales OAuth

### 3.1 Navegar a Credentials

1. En el menú lateral: **APIs & Services** → **Credentials**
2. O ve a: https://console.cloud.google.com/apis/credentials

### 3.2 Crear OAuth Client ID

1. Clic en **"+ Create Credentials"**
2. Selecciona **"OAuth client ID"**

### 3.3 Configurar Application Type

1. **Application type:** `Web application`
2. **Name:** `OKLA Web Client`

### 3.4 Configurar Authorized JavaScript Origins

Estos son los orígenes desde donde se puede iniciar el flujo OAuth.

**Para Desarrollo:**

```
http://localhost:3000
http://localhost:5173
```

**Para Producción:**

```
https://okla.com.do
https://www.okla.com.do
```

### 3.5 Configurar Authorized Redirect URIs

⚠️ **CRÍTICO:** El redirect URI debe coincidir EXACTAMENTE con lo que envía tu aplicación.

**Para Desarrollo:**

```
http://localhost:3000/auth/callback/google
```

**Para Producción:**

```
https://okla.com.do/auth/callback/google
```

### 3.6 Crear y Guardar Credenciales

1. Clic en **"Create"**
2. Se mostrará un modal con tus credenciales:
   - **Client ID:** `723958602264-xxxxx.apps.googleusercontent.com`
   - **Client Secret:** `GOCSPX-xxxxxxxxxxxxxxx`

3. **¡IMPORTANTE!** Guarda estas credenciales de forma segura:
   - Clic en **"Download JSON"** para respaldar
   - Nunca compartas el Client Secret públicamente
   - Nunca lo subas a Git

---

## Paso 4: Configurar Backend

### 4.1 Variables de Entorno en Docker

Edita el archivo `compose.yaml` y agrega las credenciales al servicio `authservice`:

```yaml
authservice:
  # ... otras configuraciones ...
  environment:
    # ... otras variables ...
    - Authentication__Google__ClientId=723958602264-xxxxx.apps.googleusercontent.com
    - Authentication__Google__ClientSecret=GOCSPX-xxxxxxxxxxxxxxx
```

### 4.2 Alternativa: Archivo appsettings.json

Si prefieres usar archivos de configuración (NO recomendado para secretos):

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "723958602264-xxxxx.apps.googleusercontent.com",
      "ClientSecret": "GOCSPX-xxxxxxxxxxxxxxx"
    }
  }
}
```

### 4.3 Verificar Configuración

El `ExternalAuthCallbackCommandHandler` lee estas configuraciones:

```csharp
var clientId = _configuration["Authentication:Google:ClientId"];
var clientSecret = _configuration["Authentication:Google:ClientSecret"];
```

### 4.4 Reiniciar AuthService

```bash
docker-compose up -d --build authservice
```

---

## Paso 5: Configurar Frontend

### 5.1 Variables de Entorno

Edita el archivo `.env.development`:

```env
VITE_GOOGLE_CLIENT_ID=723958602264-xxxxx.apps.googleusercontent.com
```

**Nota:** El Client Secret NO va en el frontend (es inseguro).

### 5.2 Verificar authService.ts

El servicio de autenticación ya está configurado para usar la variable:

```typescript
const GOOGLE_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID;
```

### 5.3 Reconstruir Frontend

```bash
docker-compose up -d --build frontend-web
```

---

## Paso 6: Probar la Integración

### 6.1 Verificar Servicios

```bash
# Verificar que authservice está corriendo
curl http://localhost:15085/health

# Verificar que el endpoint de OAuth funciona
curl -X POST http://localhost:18443/api/ExternalAuth/callback \
  -H "Content-Type: application/json" \
  -d '{"provider":"google","code":"test"}'
```

Deberías recibir un error de código inválido (esperado):

```json
{
  "success": false,
  "error": "Google OAuth token exchange failed: {\"error\": \"invalid_grant\"...}"
}
```

### 6.2 Probar Login Completo

1. Abre el navegador en **http://localhost:3000/login**
2. Haz clic en el botón **"Continuar con Google"**
3. Deberías ser redirigido a Google
4. Inicia sesión con una cuenta de test
5. Google te redirige de vuelta a la aplicación
6. Deberías estar logueado y redirigido al dashboard

### 6.3 Verificar en Base de Datos

```bash
# Conectar a PostgreSQL
docker exec -it postgres_db psql -U postgres -d authservice

# Ver usuarios creados vía OAuth
SELECT id, email, "ExternalProvider", "ExternalId"
FROM "Users"
WHERE "ExternalProvider" IS NOT NULL;
```

---

## Configuración de Producción

### 1. Publicar la App

Para salir del modo "Testing" y permitir que cualquier usuario se autentique:

1. Ve a **OAuth consent screen**
2. Clic en **"Publish App"**
3. Confirma la publicación

**Nota:** Para apps con scopes sensibles, Google requerirá verificación.

### 2. Actualizar Redirect URIs

Agrega los URIs de producción en la sección **Credentials**:

```
https://okla.com.do/auth/callback/google
https://www.okla.com.do/auth/callback/google
```

### 3. Variables de Entorno en Kubernetes

Crea un Secret para las credenciales:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: oauth-secrets
  namespace: okla
type: Opaque
stringData:
  GOOGLE_CLIENT_ID: "723958602264-xxxxx.apps.googleusercontent.com"
  GOOGLE_CLIENT_SECRET: "GOCSPX-xxxxxxxxxxxxxxx"
```

Referencia en el Deployment:

```yaml
env:
  - name: Authentication__Google__ClientId
    valueFrom:
      secretKeyRef:
        name: oauth-secrets
        key: GOOGLE_CLIENT_ID
  - name: Authentication__Google__ClientSecret
    valueFrom:
      secretKeyRef:
        name: oauth-secrets
        key: GOOGLE_CLIENT_SECRET
```

### 4. Monitoreo

Configura alertas para:

- Tasa de errores en `/api/ExternalAuth/callback`
- Latencia del endpoint
- Tokens expirados o inválidos

---

## 🔧 Troubleshooting

### Error: "redirect_uri_mismatch"

**Causa:** El redirect URI enviado no coincide con los configurados en Google Cloud.

**Solución:**

1. Verifica en Google Cloud Console que el URI esté exactamente igual
2. Revisa mayúsculas/minúsculas y trailing slashes
3. Asegúrate de incluir el protocolo (http:// o https://)

### Error: "invalid_grant"

**Causas posibles:**

1. El código ya fue usado (solo puede usarse una vez)
2. El código expiró (válido por ~10 minutos)
3. Mismatch en redirect_uri

**Solución:**

1. Intenta el flujo de nuevo desde el principio
2. Verifica que no haya double-submit en el frontend

### Error: "access_denied"

**Causa:** El usuario canceló el login o no tiene permisos.

**Solución:**

1. Verifica que el usuario esté en la lista de test users (modo Testing)
2. Publica la app para permitir cualquier usuario

### Error: "Invalid external token"

**Causa:** El token de Google no pudo ser validado.

**Solución:**

1. Verifica que el Client ID sea correcto
2. Revisa los logs del authservice para más detalles

---

## 📝 Checklist de Configuración

### Desarrollo

- [ ] Proyecto creado en Google Cloud Console
- [ ] OAuth consent screen configurado (External)
- [ ] Test users agregados
- [ ] OAuth Client ID creado (Web application)
- [ ] Authorized JavaScript origins: `http://localhost:3000`
- [ ] Authorized redirect URIs: `http://localhost:3000/auth/callback/google`
- [ ] Client ID configurado en frontend (.env.development)
- [ ] Client ID y Secret configurados en backend (compose.yaml)
- [ ] AuthService reconstruido y funcionando
- [ ] Frontend reconstruido y funcionando
- [ ] Login probado exitosamente

### Producción

- [ ] App publicada (no en Testing)
- [ ] Authorized origins incluyen dominio de producción
- [ ] Authorized redirect URIs incluyen dominio de producción
- [ ] Secrets configurados en Kubernetes
- [ ] HTTPS habilitado
- [ ] Monitoreo configurado

---

_Última actualización: Enero 22, 2026_
