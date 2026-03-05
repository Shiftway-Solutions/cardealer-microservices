# 📘 Facebook OAuth Setup - Guía Completa

Esta guía detalla el proceso para configurar Facebook Login en OKLA.

## 📋 Tabla de Contenidos

1. [Requisitos Previos](#requisitos-previos)
2. [Paso 1: Crear App en Meta for Developers](#paso-1-crear-app-en-meta-for-developers)
3. [Paso 2: Configurar Facebook Login](#paso-2-configurar-facebook-login)
4. [Paso 3: Obtener Credenciales](#paso-3-obtener-credenciales)
5. [Paso 4: Configurar Backend y Frontend](#paso-4-configurar-backend-y-frontend)
6. [Paso 5: Pasar a Producción](#paso-5-pasar-a-producción)
7. [Paso 6: Probar la Integración](#paso-6-probar-la-integración)

---

## Requisitos Previos

- ✅ Cuenta personal de Facebook
- ✅ Acceso a [Meta for Developers](https://developers.facebook.com)
- ✅ Proyecto OKLA funcionando localmente

---

## Paso 1: Crear App en Meta for Developers

### 1.1 Acceder a Meta for Developers

1. Ve a [developers.facebook.com](https://developers.facebook.com)
2. Inicia sesión con tu cuenta de Facebook
3. Si es primera vez, acepta los términos de desarrollador

### 1.2 Crear Nueva Aplicación

1. Ve a **"My Apps"** en la esquina superior derecha
2. Clic en **"Create App"**
3. Selecciona el tipo de app:
   - **"Consumer"** (para apps públicas)
4. Clic en **"Next"**

### 1.3 Configurar Información Básica

| Campo             | Valor                                 |
| ----------------- | ------------------------------------- |
| App Name          | `OKLA`                                |
| App Contact Email | `tu-email@ejemplo.com`                |
| Business Account  | (Opcional, para empresas verificadas) |

5. Clic en **"Create App"**
6. Completa la verificación de seguridad si se solicita

---

## Paso 2: Configurar Facebook Login

### 2.1 Agregar Producto Facebook Login

1. En el Dashboard de tu app, ve a **"Add Products to Your App"**
2. Busca **"Facebook Login"**
3. Clic en **"Set Up"**
4. Selecciona **"Web"**

### 2.2 Configurar Site URL

1. En el campo **"Site URL"**, ingresa:
   ```
   http://localhost:3000
   ```
2. Clic en **"Save"**
3. Clic en **"Continue"**

### 2.3 Configurar OAuth Redirect URIs

1. Ve a **Facebook Login > Settings** en el menú lateral
2. En **"Valid OAuth Redirect URIs"**, agrega:

   ```
   http://localhost:3000/auth/callback/facebook
   ```

   **Para producción (agregar después):**

   ```
   https://okla.com.do/auth/callback/facebook
   ```

3. Clic en **"Save Changes"**

### 2.4 Configurar Opciones Adicionales

En la misma página de Settings, configura:

| Opción                           | Valor                  |
| -------------------------------- | ---------------------- |
| Client OAuth Login               | ✅ Yes                 |
| Web OAuth Login                  | ✅ Yes                 |
| Force Web OAuth Reauthentication | ❌ No                  |
| Enforce HTTPS                    | ✅ Yes (en producción) |
| Embedded Browser OAuth Login     | ❌ No                  |
| Login with the JavaScript SDK    | ❌ No                  |

Clic en **"Save Changes"**

---

## Paso 3: Obtener Credenciales

### 3.1 Navegar a Basic Settings

1. Ve a **Settings > Basic** en el menú lateral

### 3.2 Obtener App ID y App Secret

Copia y guarda:

- **App ID:** `123456789012345`
- **App Secret:** Clic en **"Show"**, ingresa tu contraseña de Facebook, y copia el secret

⚠️ **IMPORTANTE:** Nunca compartas ni commits el App Secret.

### 3.3 Configurar Información de la App (para producción)

| Campo                | Valor                                        |
| -------------------- | -------------------------------------------- |
| Display Name         | `OKLA - Compra y Venta de Vehículos`         |
| App Domains          | `localhost`, `okla.com.do` (para producción) |
| Privacy Policy URL   | `https://okla.com.do/privacy`                |
| Terms of Service URL | `https://okla.com.do/terms`                  |
| App Icon             | (Sube el logo de OKLA)                       |

---

## Paso 4: Configurar Backend y Frontend

### 4.1 Backend (compose.yaml)

```yaml
authservice:
  environment:
    - Authentication__Facebook__ClientId=123456789012345
    - Authentication__Facebook__ClientSecret=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

### 4.2 Frontend (.env.development)

```env
VITE_FACEBOOK_CLIENT_ID=123456789012345
```

### 4.3 Reiniciar Servicios

```bash
docker-compose up -d --build authservice frontend-web
```

---

## Paso 5: Pasar a Producción

### 5.1 Cambiar App Mode

1. En el Dashboard de tu app, arriba hay un toggle **"App Mode"**
2. Actualmente está en **"Development"**
3. Para producción, cámbialo a **"Live"**

### 5.2 Requisitos para Modo Live

Antes de cambiar a Live, asegúrate de tener:

- ✅ Privacy Policy URL configurada
- ✅ Data Deletion Instructions URL (o callback)
- ✅ App Icon subido
- ✅ App verificada (puede requerir Business Verification)

### 5.3 Configurar Data Deletion

Facebook requiere que proporciones una forma para que los usuarios eliminen sus datos.

1. Ve a **Settings > Basic**
2. En **"Data Deletion"**, elige:
   - **Data Deletion Callback URL:** `https://api.okla.com.do/api/auth/facebook/data-deletion`
   - O **Data Deletion Instructions URL:** `https://okla.com.do/help/delete-account`

---

## Paso 6: Probar la Integración

### 6.1 Modo Development

En modo Development, solo los usuarios agregados como **Testers** o **Developers** pueden usar el login.

Para agregar testers:

1. Ve a **Roles > Roles**
2. Clic en **"Add Testers"**
3. Ingresa el Facebook ID o nombre del tester

### 6.2 Probar Login

1. Abre http://localhost:3000/login
2. Clic en **"Continuar con Facebook"**
3. Inicia sesión con una cuenta de tester
4. Acepta los permisos
5. Deberías ser redirigido y logueado

---

## 🔧 Troubleshooting

### Error: "App Not Setup"

**Causa:** La app está en modo Development y el usuario no es un tester.

**Solución:**

- Agregar al usuario como Tester en Roles
- O cambiar la app a modo Live (requiere requisitos)

### Error: "URL Blocked"

**Causa:** El redirect URI no está en la lista de Valid OAuth Redirect URIs.

**Solución:** Agregar el URI exacto en Facebook Login > Settings.

### Error: "Invalid App ID"

**Causa:** El App ID configurado en el frontend/backend no coincide.

**Solución:** Verificar que el App ID es correcto en las variables de entorno.

### Error: "Can't Load URL"

**Causa:** El dominio no está configurado en App Domains.

**Solución:** Agregar el dominio en Settings > Basic > App Domains.

---

## 📋 Permisos Requeridos

Para el login básico, OKLA solicita:

| Permiso          | Descripción        | Review Required |
| ---------------- | ------------------ | --------------- |
| `email`          | Email del usuario  | ❌ No           |
| `public_profile` | Nombre, foto, etc. | ❌ No           |

Estos permisos básicos no requieren App Review.

---

## 📋 Checklist

- [ ] App creada en Meta for Developers
- [ ] Facebook Login configurado
- [ ] App ID copiado
- [ ] App Secret copiado y guardado de forma segura
- [ ] Valid OAuth Redirect URI configurado
- [ ] Backend configurado con App ID y Secret
- [ ] Frontend configurado con App ID
- [ ] Usuario agregado como Tester (para modo Development)
- [ ] Servicios reiniciados
- [ ] Login probado exitosamente
- [ ] (Producción) Privacy Policy configurada
- [ ] (Producción) App cambiada a modo Live

---

_Última actualización: Enero 22, 2026_
