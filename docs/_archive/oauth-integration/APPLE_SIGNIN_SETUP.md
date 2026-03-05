# 🍎 Apple Sign In Setup - Guía Completa

Esta guía detalla el proceso para configurar Sign In with Apple en OKLA.

## 📋 Tabla de Contenidos

1. [Requisitos Previos](#requisitos-previos)
2. [Paso 1: Registrar App ID](#paso-1-registrar-app-id)
3. [Paso 2: Crear Service ID](#paso-2-crear-service-id)
4. [Paso 3: Generar Key](#paso-3-generar-key)
5. [Paso 4: Configurar Domains y Redirect URIs](#paso-4-configurar-domains-y-redirect-uris)
6. [Paso 5: Generar Client Secret](#paso-5-generar-client-secret)
7. [Paso 6: Configurar Backend y Frontend](#paso-6-configurar-backend-y-frontend)
8. [Paso 7: Probar la Integración](#paso-7-probar-la-integración)

---

## Requisitos Previos

- ✅ Apple Developer Account (cuesta $99/año)
- ✅ Acceso a [Apple Developer Portal](https://developer.apple.com)
- ✅ Proyecto OKLA funcionando localmente
- ✅ Dominio con HTTPS (Apple requiere HTTPS incluso para desarrollo)

⚠️ **NOTA:** Apple Sign In requiere HTTPS. Para desarrollo local, necesitarás:

- Un túnel como ngrok: `ngrok http 3000`
- O configurar certificado SSL local

---

## Paso 1: Registrar App ID

### 1.1 Acceder a Apple Developer

1. Ve a [developer.apple.com](https://developer.apple.com)
2. Inicia sesión con tu Apple ID de desarrollador
3. Ve a **"Certificates, Identifiers & Profiles"**

### 1.2 Crear App ID

1. En el menú lateral, clic en **"Identifiers"**
2. Clic en el botón **"+"**
3. Selecciona **"App IDs"** y clic en **"Continue"**
4. Selecciona **"App"** y clic en **"Continue"**

### 1.3 Configurar App ID

| Campo       | Valor                     |
| ----------- | ------------------------- |
| Description | `OKLA Web App`            |
| Bundle ID   | `com.okla.web` (Explicit) |

5. En **"Capabilities"**, scroll down y marca:
   - ✅ **Sign In with Apple**

6. Clic en **"Continue"** y luego **"Register"**

---

## Paso 2: Crear Service ID

### 2.1 Crear Nuevo Service ID

1. En **"Identifiers"**, clic en **"+"**
2. Selecciona **"Services IDs"** y clic en **"Continue"**

### 2.2 Configurar Service ID

| Campo       | Valor                 |
| ----------- | --------------------- |
| Description | `OKLA Web Sign In`    |
| Identifier  | `com.okla.web.signin` |

3. Clic en **"Continue"** y luego **"Register"**

### 2.3 Configurar Sign In with Apple

1. Encuentra tu Service ID recién creado en la lista
2. Clic en él para editar
3. Marca ✅ **"Sign In with Apple"**
4. Clic en **"Configure"**

---

## Paso 3: Generar Key

### 3.1 Crear Key

1. En el menú lateral, clic en **"Keys"**
2. Clic en el botón **"+"**

### 3.2 Configurar Key

| Campo    | Valor              |
| -------- | ------------------ |
| Key Name | `OKLA Sign In Key` |

3. Marca ✅ **"Sign In with Apple"**
4. Clic en **"Configure"** al lado de Sign In with Apple
5. Selecciona tu App ID (`com.okla.web`) como Primary App ID
6. Clic en **"Save"**
7. Clic en **"Continue"** y luego **"Register"**

### 3.3 Descargar la Key

⚠️ **MUY IMPORTANTE:** Solo puedes descargar la key UNA VEZ.

1. Clic en **"Download"** para descargar el archivo `.p8`
2. Guarda el archivo de forma segura: `AuthKey_XXXXXXXXXX.p8`
3. Anota el **Key ID** que se muestra en la página

**Contenido del archivo .p8 (ejemplo):**

```
-----BEGIN PRIVATE KEY-----
MIGTAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBHkwdwIBAQQg...
...
-----END PRIVATE KEY-----
```

---

## Paso 4: Configurar Domains y Redirect URIs

### 4.1 Configurar Service ID

1. Vuelve a **"Identifiers"**
2. Cambia el filtro a **"Services IDs"**
3. Clic en tu Service ID (`com.okla.web.signin`)
4. Clic en **"Configure"** al lado de Sign In with Apple

### 4.2 Agregar Domains

En **"Domains and Subdomains"**, agrega:

**Para desarrollo (con ngrok):**

```
xxxx-xxx-xxx-xxx-xxx.ngrok-free.app
```

**Para producción:**

```
okla.com.do
api.okla.com.do
```

### 4.3 Agregar Return URLs

En **"Return URLs"**, agrega:

**Para desarrollo:**

```
https://xxxx-xxx-xxx-xxx-xxx.ngrok-free.app/auth/callback/apple
```

**Para producción:**

```
https://okla.com.do/auth/callback/apple
```

5. Clic en **"Next"** y luego **"Done"**
6. Clic en **"Continue"** y luego **"Save"**

---

## Paso 5: Generar Client Secret

Apple no usa un client secret estático. En su lugar, debes generar un JWT firmado con tu private key.

### 5.1 Información Necesaria

Recopila estos valores:

| Campo       | Valor                 | Dónde encontrarlo              |
| ----------- | --------------------- | ------------------------------ |
| Team ID     | `XXXXXXXXXX`          | Developer Account > Membership |
| Key ID      | `XXXXXXXXXX`          | Keys > Tu Key                  |
| Service ID  | `com.okla.web.signin` | El identifier que creaste      |
| Private Key | Contenido del `.p8`   | El archivo que descargaste     |

### 5.2 Generar JWT con Node.js

Crea un script para generar el client secret:

```javascript
// generate-apple-secret.js
const jwt = require("jsonwebtoken");
const fs = require("fs");

const teamId = "XXXXXXXXXX"; // Tu Team ID
const keyId = "XXXXXXXXXX"; // Tu Key ID
const serviceId = "com.okla.web.signin";
const privateKey = fs.readFileSync("./AuthKey_XXXXXXXXXX.p8");

const token = jwt.sign({}, privateKey, {
  algorithm: "ES256",
  expiresIn: "180d",
  audience: "https://appleid.apple.com",
  issuer: teamId,
  subject: serviceId,
  keyid: keyId,
});

console.log("Client Secret:");
console.log(token);
```

```bash
npm install jsonwebtoken
node generate-apple-secret.js
```

### 5.3 Almacenar el Client Secret

El JWT generado tiene una validez máxima de 6 meses. Deberás regenerarlo antes de que expire.

---

## Paso 6: Configurar Backend y Frontend

### 6.1 Backend (compose.yaml)

```yaml
authservice:
  environment:
    - Authentication__Apple__ClientId=com.okla.web.signin
    - Authentication__Apple__TeamId=XXXXXXXXXX
    - Authentication__Apple__KeyId=XXXXXXXXXX
    - Authentication__Apple__PrivateKey=-----BEGIN PRIVATE KEY-----\nMIGTAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBHkwdwIBAQQg...\n-----END PRIVATE KEY-----
```

⚠️ **NOTA:** Para la Private Key, reemplaza los saltos de línea con `\n`.

### 6.2 Frontend (.env.development)

```env
VITE_APPLE_CLIENT_ID=com.okla.web.signin
```

### 6.3 Configurar Backend para Generar Client Secret

En el backend, el client secret debe generarse dinámicamente:

```csharp
// AppleClientSecretGenerator.cs
public class AppleClientSecretGenerator
{
    private readonly IConfiguration _configuration;

    public AppleClientSecretGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateClientSecret()
    {
        var teamId = _configuration["Authentication:Apple:TeamId"];
        var clientId = _configuration["Authentication:Apple:ClientId"];
        var keyId = _configuration["Authentication:Apple:KeyId"];
        var privateKey = _configuration["Authentication:Apple:PrivateKey"];

        var now = DateTime.UtcNow;
        var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(Convert.FromBase64String(
            privateKey
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("\n", "")), out _);

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = teamId,
            Audience = "https://appleid.apple.com",
            Subject = new ClaimsIdentity(new[] { new Claim("sub", clientId) }),
            Expires = now.AddMonths(6),
            IssuedAt = now,
            NotBefore = now,
            SigningCredentials = new SigningCredentials(
                new ECDsaSecurityKey(ecdsa) { KeyId = keyId },
                SecurityAlgorithms.EcdsaSha256)
        });

        return token;
    }
}
```

### 6.4 Reiniciar Servicios

```bash
docker-compose up -d --build authservice frontend-web
```

---

## Paso 7: Probar la Integración

### 7.1 Iniciar Túnel HTTPS (para desarrollo)

```bash
ngrok http 3000
```

Copia la URL HTTPS generada (ej: `https://xxxx.ngrok-free.app`)

### 7.2 Actualizar URLs

1. Actualiza el frontend para usar la URL de ngrok
2. Actualiza el redirect URI en Apple Developer Portal

### 7.3 Probar Login

1. Abre `https://xxxx.ngrok-free.app/login`
2. Clic en **"Continuar con Apple"**
3. Inicia sesión con tu Apple ID
4. Decide si ocultar tu email (opción de Apple)
5. Deberías ser redirigido y logueado

---

## 🔧 Troubleshooting

### Error: "invalid_client"

**Causas comunes:**

1. Client Secret expirado o mal generado
2. Service ID incorrecto
3. Key no asociada correctamente

**Solución:** Regenerar el client secret y verificar configuración.

### Error: "invalid_grant"

**Causa:** El authorization code ya fue usado o expiró.

**Solución:** Asegurarte de que el código se use solo una vez e inmediatamente.

### Error: "redirect_uri_mismatch"

**Causa:** El redirect URI no coincide con los registrados.

**Solución:** Verificar que el URI exacto esté en los Return URLs del Service ID.

### Error: "unsupported_grant_type"

**Causa:** El request al token endpoint está mal formado.

**Solución:** Verificar que se está enviando `grant_type=authorization_code`.

---

## 🔒 Consideraciones de Privacidad de Apple

### Hide My Email

Apple permite a los usuarios ocultar su email real. En ese caso, recibirás un email relay como:

```
xyz123abc@privaterelay.appleid.com
```

Tu app debe:

1. Aceptar estos emails como válidos
2. Poder enviar emails a estas direcciones relay

### Primera vez vs. Logins posteriores

Apple solo envía el nombre y email del usuario la **primera vez** que autoriza tu app.

**Importante:** Debes guardar esta información en el primer login, porque en logins posteriores no la recibirás.

---

## 📋 Resumen de Credenciales

| Credencial                 | Valor                 | Ubicación en Apple         |
| -------------------------- | --------------------- | -------------------------- |
| **Team ID**                | `XXXXXXXXXX`          | Membership > Team ID       |
| **Service ID (Client ID)** | `com.okla.web.signin` | Identifiers > Services IDs |
| **Key ID**                 | `XXXXXXXXXX`          | Keys > Tu Key              |
| **Private Key**            | Archivo `.p8`         | Descargado al crear Key    |

---

## 📋 Checklist

- [ ] Apple Developer Account activo ($99/año)
- [ ] App ID creado con Sign In with Apple habilitado
- [ ] Service ID creado y configurado
- [ ] Key creada y archivo .p8 descargado
- [ ] Domains configurados en Service ID
- [ ] Return URLs configurados
- [ ] Client Secret generado (JWT)
- [ ] Backend configurado con todas las credenciales
- [ ] Frontend configurado con Client ID
- [ ] Túnel HTTPS configurado (para desarrollo)
- [ ] Servicios reiniciados
- [ ] Login probado exitosamente

---

## ⚠️ Notas Importantes

1. **Renovar Client Secret:** El JWT tiene validez máxima de 6 meses. Configura un reminder para regenerarlo.

2. **HTTPS Obligatorio:** Apple no permite HTTP, ni siquiera para desarrollo local.

3. **Guardadr datos del primer login:** El nombre y email solo vienen la primera vez.

4. **Emails relay:** Acepta y soporta emails `@privaterelay.appleid.com`.

5. **Bundle ID vs Service ID:** Para web usa Service ID, para iOS/macOS usa el Bundle ID de la app.

---

_Última actualización: Enero 22, 2026_
