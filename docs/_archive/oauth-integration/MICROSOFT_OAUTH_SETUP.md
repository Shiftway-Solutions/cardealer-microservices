# 🔵 Microsoft OAuth Setup - Guía Completa

Esta guía detalla el proceso para configurar Microsoft/Azure AD OAuth en OKLA.

## 📋 Tabla de Contenidos

1. [Requisitos Previos](#requisitos-previos)
2. [Paso 1: Registrar Aplicación en Azure](#paso-1-registrar-aplicación-en-azure)
3. [Paso 2: Configurar Authentication](#paso-2-configurar-authentication)
4. [Paso 3: Crear Client Secret](#paso-3-crear-client-secret)
5. [Paso 4: Configurar API Permissions](#paso-4-configurar-api-permissions)
6. [Paso 5: Configurar Backend y Frontend](#paso-5-configurar-backend-y-frontend)
7. [Paso 6: Probar la Integración](#paso-6-probar-la-integración)

---

## Requisitos Previos

- ✅ Cuenta de Microsoft (personal, trabajo o escuela)
- ✅ Acceso a [Azure Portal](https://portal.azure.com) o [Entra Admin Center](https://entra.microsoft.com)
- ✅ Proyecto OKLA funcionando localmente

---

## Paso 1: Registrar Aplicación en Azure

### 1.1 Acceder a Azure Portal

1. Ve a [portal.azure.com](https://portal.azure.com)
2. Inicia sesión con tu cuenta de Microsoft

### 1.2 Navegar a App Registrations

1. En la barra de búsqueda, escribe "App registrations"
2. Selecciona **"App registrations"** en los resultados
3. O ve directamente a: https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade

### 1.3 Crear Nueva Aplicación

1. Clic en **"+ New registration"**
2. Configura:

| Campo                   | Valor                                                                        | Notas                                    |
| ----------------------- | ---------------------------------------------------------------------------- | ---------------------------------------- |
| Name                    | `OKLA`                                                                       | Nombre visible para usuarios             |
| Supported account types | **Accounts in any organizational directory and personal Microsoft accounts** | Para permitir cualquier cuenta Microsoft |
| Redirect URI (optional) | Web: `http://localhost:3000/auth/callback/microsoft`                         | Para desarrollo                          |

3. Clic en **"Register"**

### 1.4 Guardar Application ID

Después de crear la app, verás el **Overview**. Guarda estos valores:

- **Application (client) ID:** `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`
- **Directory (tenant) ID:** (solo si es necesario para tu caso)

---

## Paso 2: Configurar Authentication

### 2.1 Navegar a Authentication

1. En el menú lateral de tu app, clic en **"Authentication"**

### 2.2 Configurar Platform Settings

1. En **"Platform configurations"**, clic en **"+ Add a platform"**
2. Selecciona **"Web"**
3. Configura:

**Redirect URIs:**

```
http://localhost:3000/auth/callback/microsoft
```

**Para producción (agregar después):**

```
https://okla.com.do/auth/callback/microsoft
```

**Front-channel logout URL:** (opcional)

```
http://localhost:3000/logout
```

**Implicit grant and hybrid flows:**

- ✅ Access tokens
- ✅ ID tokens

4. Clic en **"Configure"**

### 2.3 Configurar Supported Account Types

Verifica que en **"Supported account types"** esté seleccionado:

- **Accounts in any organizational directory and personal Microsoft accounts**

---

## Paso 3: Crear Client Secret

### 3.1 Navegar a Certificates & Secrets

1. En el menú lateral, clic en **"Certificates & secrets"**

### 3.2 Crear Nuevo Secret

1. En la pestaña **"Client secrets"**, clic en **"+ New client secret"**
2. Configura:

| Campo       | Valor                           |
| ----------- | ------------------------------- |
| Description | `OKLA OAuth Secret`             |
| Expires     | 24 months (o según tu política) |

3. Clic en **"Add"**

### 3.3 Guardar el Secret

⚠️ **IMPORTANTE:** El valor del secret solo se muestra UNA VEZ.

Copia y guarda de forma segura:

- **Value:** `xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`
- **Secret ID:** `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`

---

## Paso 4: Configurar API Permissions

### 4.1 Navegar a API Permissions

1. En el menú lateral, clic en **"API permissions"**

### 4.2 Verificar/Agregar Permisos

Por defecto, debería tener `User.Read`. Si no, agrégalo:

1. Clic en **"+ Add a permission"**
2. Selecciona **"Microsoft Graph"**
3. Selecciona **"Delegated permissions"**
4. Busca y selecciona:
   - ✅ `openid`
   - ✅ `email`
   - ✅ `profile`
   - ✅ `User.Read`

5. Clic en **"Add permissions"**

### 4.3 Grant Admin Consent (si es necesario)

Si ves un warning de "Not granted for...", y tienes permisos de admin:

1. Clic en **"Grant admin consent for [Directory]"**
2. Confirma

---

## Paso 5: Configurar Backend y Frontend

### 5.1 Backend (compose.yaml)

```yaml
authservice:
  environment:
    - Authentication__Microsoft__ClientId=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
    - Authentication__Microsoft__ClientSecret=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

### 5.2 Frontend (.env.development)

```env
VITE_MICROSOFT_CLIENT_ID=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

### 5.3 Reiniciar Servicios

```bash
docker-compose up -d --build authservice frontend-web
```

---

## Paso 6: Probar la Integración

### 6.1 Verificar Endpoint

```bash
curl -X POST http://localhost:18443/api/ExternalAuth/callback \
  -H "Content-Type: application/json" \
  -d '{"provider":"microsoft","code":"test"}'
```

### 6.2 Probar Login

1. Abre http://localhost:3000/login
2. Clic en **"Continuar con Microsoft"**
3. Inicia sesión con tu cuenta Microsoft
4. Deberías ser redirigido y logueado

---

## 🔧 Troubleshooting

### Error: "AADSTS50011: The redirect URI is not registered"

**Solución:** Agregar el redirect URI exacto en Azure → Authentication → Redirect URIs.

### Error: "AADSTS7000218: The request body must contain: client_secret"

**Solución:** Verificar que el Client Secret está configurado en el backend.

### Error: "AADSTS65001: The user or administrator has not consented"

**Solución:** El usuario debe aceptar los permisos, o el admin debe grant consent.

---

## 📋 Checklist

- [ ] App registrada en Azure
- [ ] Application (client) ID copiado
- [ ] Client Secret creado y guardado
- [ ] Redirect URI configurado: `http://localhost:3000/auth/callback/microsoft`
- [ ] API permissions configurados (openid, email, profile, User.Read)
- [ ] Backend configurado con Client ID y Secret
- [ ] Frontend configurado con Client ID
- [ ] Servicios reiniciados
- [ ] Login probado exitosamente

---

_Última actualización: Enero 22, 2026_
