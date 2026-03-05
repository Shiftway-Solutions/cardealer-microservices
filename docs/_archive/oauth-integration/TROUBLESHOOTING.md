# 🔧 OAuth Integration - Troubleshooting

Guía de solución de problemas comunes en la integración OAuth.

## 📋 Índice

1. [Errores de Configuración](#errores-de-configuración)
2. [Errores de Redirect URI](#errores-de-redirect-uri)
3. [Errores de Token](#errores-de-token)
4. [Errores de Frontend](#errores-de-frontend)
5. [Errores de Backend](#errores-de-backend)
6. [Herramientas de Diagnóstico](#herramientas-de-diagnóstico)

---

## Errores de Configuración

### ❌ Error: "Google OAuth credentials are not configured"

**Causa:** El Client ID o Client Secret no están configurados en el backend.

**Diagnóstico:**

```bash
# Verificar variables en el contenedor
docker exec authservice printenv | grep -i google
```

**Solución:**

```yaml
# compose.yaml - authservice
environment:
  - Authentication__Google__ClientId=xxx.apps.googleusercontent.com
  - Authentication__Google__ClientSecret=GOCSPX-xxx
```

```bash
# Reiniciar el servicio
docker-compose up -d --build authservice
```

---

### ❌ Error: "The OAuth client was not found"

**Causa:** El Client ID no existe en Google Cloud.

**Diagnóstico:**

1. Ve a [Google Cloud Console → Credentials](https://console.cloud.google.com/apis/credentials)
2. Verifica que el Client ID exista y esté activo

**Solución:**

- Si el Client ID no existe, créalo siguiendo [GOOGLE_OAUTH_SETUP.md](./GOOGLE_OAUTH_SETUP.md)
- Si existe, verifica que copiaste el ID correctamente (sin espacios)

---

## Errores de Redirect URI

### ❌ Error: "redirect_uri_mismatch"

**Causa:** El redirect_uri enviado no coincide con los configurados en Google Cloud.

**Mensaje completo:**

```
Error 400: redirect_uri_mismatch
The redirect URI in the request, http://localhost:3000/auth/callback/google,
does not match the ones authorized for the OAuth client.
```

**Diagnóstico:**

1. Ve a [Google Cloud Console → Credentials](https://console.cloud.google.com/apis/credentials)
2. Edita tu OAuth Client ID
3. Revisa la sección "Authorized redirect URIs"

**Checklist de verificación:**

| Aspecto        | Verificar                      |
| -------------- | ------------------------------ |
| Protocolo      | `http://` vs `https://`        |
| Host           | `localhost` vs `127.0.0.1`     |
| Puerto         | `3000` vs `5173` vs sin puerto |
| Path           | `/auth/callback/google` exacto |
| Trailing slash | Sin `/` al final               |

**Solución:**
Agregar el URI exacto en Google Cloud Console:

```
http://localhost:3000/auth/callback/google
```

---

### ❌ Error: "Access blocked: This app's request is invalid"

**Causa:** Falta configurar JavaScript origins.

**Solución:**
En Google Cloud Console, agregar a "Authorized JavaScript origins":

```
http://localhost:3000
```

---

## Errores de Token

### ❌ Error: "invalid_grant" + "Malformed auth code"

**Causa:** El código de autorización está mal formado o fue manipulado.

**Diagnóstico:**

```bash
# Ver logs del authservice
docker logs authservice 2>&1 | grep -i "invalid_grant" | tail -5
```

**Posibles causas:**

1. El código fue truncado en la URL
2. El código contiene caracteres especiales que no se codificaron correctamente

**Solución:**

- Verificar que el frontend extrae el código correctamente
- Asegurar que no hay encoding/decoding extra

---

### ❌ Error: "invalid_grant" + "Bad Request"

**Causa:** El redirect_uri enviado en el token exchange no coincide con el usado en la autorización.

**Diagnóstico:**

```bash
# Ver qué redirect_uri se está enviando
docker logs authservice 2>&1 | grep -i redirect | tail -10
```

**Verificación:**
El redirect_uri DEBE ser idéntico en ambos lugares:

1. **Frontend (authService.ts):**

```typescript
const redirectUri = `${window.location.origin}/auth/callback/${provider}`;
// Resultado: http://localhost:3000/auth/callback/google
```

2. **Backend (ExternalAuthCallbackCommandHandler.cs):**

```csharp
["redirect_uri"] = redirectUri ?? ""
```

**Solución:**
Asegurar que el frontend envía el redirect_uri correcto en el body:

```json
{
  "provider": "google",
  "code": "xxx",
  "redirectUri": "http://localhost:3000/auth/callback/google"
}
```

---

### ❌ Error: "invalid_grant" + "Code was already redeemed"

**Causa:** El código ya fue usado (solo puede usarse una vez).

**Diagnóstico:**
El frontend está haciendo múltiples requests con el mismo código.

**Solución:**
Ya implementada con `useRef` en `OAuthCallbackPage.tsx`:

```typescript
const hasProcessed = useRef(false);

useEffect(() => {
  if (hasProcessed.current) return;
  hasProcessed.current = true;
  // ... procesar callback
}, [...]);
```

---

### ❌ Error: "Invalid external token"

**Causa:** El id_token de Google no pudo ser validado.

**Diagnóstico:**

```bash
# Ver error específico en logs
docker logs authservice 2>&1 | grep -A5 "Error validating Google token"
```

**Posibles causas:**

1. **Error de deserialización JSON:**

   ```
   JsonException: The JSON value could not be converted to System.Boolean
   ```

   **Solución:** El campo `email_verified` debe ser string, no bool:

   ```csharp
   public string email_verified { get; set; } = string.Empty;
   ```

2. **Token expirado:**
   Los tokens expiran rápidamente (~5 minutos).

   **Solución:** Intentar el flujo de nuevo desde el principio.

3. **Token para otro Client ID:**
   El token fue emitido para un Client ID diferente.

   **Solución:** Verificar que el Client ID en frontend y backend coincidan.

---

## Errores de Frontend

### ❌ Error: "Failed to load resource: 404 (Not Found)" en callback

**Causa:** El Gateway no tiene la ruta configurada.

**Diagnóstico:**

```bash
# Verificar configuración de Ocelot
grep -A5 "ExternalAuth" backend/Gateway/Gateway.Api/ocelot.dev.json
```

**Solución:**
Agregar ruta en `ocelot.dev.json`:

```json
{
  "UpstreamPathTemplate": "/api/ExternalAuth/{everything}",
  "UpstreamHttpMethod": ["OPTIONS", "GET", "POST", "PUT", "DELETE"],
  "DownstreamPathTemplate": "/api/ExternalAuth/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "authservice", "Port": 80 }]
}
```

```bash
# Reiniciar gateway
docker-compose up -d --build gateway-service
```

---

### ❌ Error: Console muestra warnings de extensiones

**Mensajes:**

```
web-client-content-script.js: Failed to execute 'observe' on 'MutationObserver'
sw.js: Failed to execute 'put' on 'Cache': Request scheme 'chrome-extension' is unsupported
```

**Causa:** Extensiones del navegador (1Password, etc.) intentando interceptar.

**Solución:**

- Estos warnings son normales y no afectan la funcionalidad
- Si causan problemas, probar en modo incógnito sin extensiones

---

### ❌ Error: Double-submit del código

**Síntomas:**

- Logs muestran dos requests simultáneos
- Segundo request falla con "Code was already redeemed"

**Causa:** React StrictMode ejecuta efectos dos veces en desarrollo.

**Solución:**
Usar `useRef` para prevenir doble ejecución:

```typescript
const hasProcessed = useRef(false);

useEffect(() => {
  if (hasProcessed.current) return;
  hasProcessed.current = true;

  handleCallback();
}, []);
```

---

## Errores de Backend

### ❌ Error: "No se puede resolver IHttpClientFactory"

**Causa:** `HttpClient` no está registrado en DI.

**Solución:**
En `Program.cs`:

```csharp
builder.Services.AddHttpClient();
```

---

### ❌ Error: "The request was aborted due to timeout"

**Causa:** El backend no puede conectar con Google (firewall, DNS, etc.).

**Diagnóstico:**

```bash
# Probar conectividad desde el contenedor
docker exec authservice curl -I https://oauth2.googleapis.com/token
```

**Solución:**

- Verificar configuración de red de Docker
- Verificar que no hay proxy bloqueando

---

## Herramientas de Diagnóstico

### Ver logs en tiempo real

```bash
# Logs del authservice
docker logs -f authservice 2>&1 | grep -i "external\|oauth\|google"

# Logs del gateway
docker logs -f gateway-service 2>&1 | grep -i "externalauth"
```

### Probar endpoints manualmente

```bash
# Probar que el endpoint existe
curl -X POST http://localhost:18443/api/ExternalAuth/callback \
  -H "Content-Type: application/json" \
  -d '{"provider":"google","code":"test"}' \
  | jq .

# Esperado: Error de código inválido (pero endpoint funciona)
```

### Verificar configuración del Gateway

```bash
# Ver todas las rutas de auth
grep -B2 -A10 "ExternalAuth\|authservice" backend/Gateway/Gateway.Api/ocelot.dev.json
```

### Verificar variables de entorno

```bash
# Ver config de authservice
docker exec authservice printenv | grep -i "auth\|google\|microsoft"
```

### Verificar conectividad de red

```bash
# Desde dentro del contenedor
docker exec authservice curl -s https://oauth2.googleapis.com/tokeninfo | head -1
```

### Debug de tokens JWT

Para inspeccionar un token JWT:

1. Ve a [jwt.io](https://jwt.io)
2. Pega el token
3. Verifica el payload (claims)

---

## 📋 Checklist de Debugging

Cuando algo falla, verificar en orden:

- [ ] 1. ¿El servicio authservice está corriendo? `docker ps | grep auth`
- [ ] 2. ¿El health check funciona? `curl localhost:15085/health`
- [ ] 3. ¿Las variables de entorno están configuradas? `docker exec authservice printenv | grep GOOGLE`
- [ ] 4. ¿El Gateway tiene la ruta? `grep ExternalAuth ocelot.dev.json`
- [ ] 5. ¿El endpoint responde a través del Gateway? `curl localhost:18443/api/ExternalAuth/callback`
- [ ] 6. ¿Los redirect URIs están configurados en Google Cloud?
- [ ] 7. ¿El usuario está en la lista de test users? (modo Testing)
- [ ] 8. ¿Hay errores en los logs? `docker logs authservice 2>&1 | tail -50`

---

_Última actualización: Enero 22, 2026_
