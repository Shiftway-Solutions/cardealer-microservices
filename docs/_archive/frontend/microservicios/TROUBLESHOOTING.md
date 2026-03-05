# 🐛 Troubleshooting: Problemas Comunes & Soluciones

**Versión:** 1.0  
**Actualizado:** Enero 18, 2026  
**Audience:** Todos los desarrolladores

---

## 🆘 Problema: 404 en Endpoint

### ❌ Síntoma

```
GET /api/vehicles
Error: 404 Not Found
```

### 🔍 Diagnóstico

1. **Verifica el nombre del servicio**

   ```typescript
   // En MICROSERVICIOS_GUIA_RAPIDA.md busca:
   // VehiclesSaleService → /api/vehicles
   ```

2. **Verifica que el servicio esté corriendo**

   ```bash
   curl -i https://api.okla.com.do/health
   # Debe retornar 200 OK
   ```

3. **Verifica la ruta en Gateway**
   - Archivo: `backend/Gateway/Gateway.Api/ocelot.prod.json`
   - Busca: `"DownstreamPathTemplate": "/api/vehicles..."`
   - Debe tener: `"UpstreamPathTemplate": "/api/vehicles..."`

4. **Verifica que no hayas escrito mal**

   ```typescript
   // ❌ MAL
   /api/acceeeeehiillrssvv /
     list /
     // ✅ CORRECTO
     api /
     vehicles;
   ```

### ✅ Solución

```bash
# Opción 1: Verificar en local
curl -i http://localhost:18443/api/vehicles

# Opción 2: Ver logs del servicio en K8s
kubectl logs deployment/vehiclessaleservice -n okla

# Opción 3: Verificar ocelot config
kubectl get configmap gateway-config -n okla -o yaml | grep -A10 vehicles
```

---

## 🆘 Problema: 401 Unauthorized

### ❌ Síntoma

```json
{
  "statusCode": 401,
  "message": "Unauthorized"
}
```

### 🔍 Diagnóstico

1. **¿Incluiste el token?**

   ```typescript
   // ❌ MAL - Sin Authorization header
   await apiClient.get("/api/vehicles");

   // ✅ CORRECTO - Con token
   const token = localStorage.getItem("accessToken");
   await apiClient.get("/api/vehicles", {
     headers: {
       Authorization: `Bearer ${token}`,
     },
   });
   ```

2. **¿El token está expirado?**

   ```typescript
   // Verifica en localStorage
   localStorage.getItem("accessToken");

   // Decodifica el JWT (en browser console)
   JSON.parse(atob(token.split(".")[1]));
   // Mira "exp" - timestamp de expiración
   ```

3. **¿El interceptor Axios está configurado?**
   ```typescript
   // Debe estar en axiosConfig.ts
   apiClient.interceptors.request.use((config) => {
     const token = localStorage.getItem("accessToken");
     if (token) {
       config.headers.Authorization = `Bearer ${token}`;
     }
     return config;
   });
   ```

### ✅ Solución

```typescript
// Opción 1: Login nuevamente
await authService.login({
  email: "user@example.com",
  password: "password",
});

// Opción 2: Refresh token si está disponible
try {
  const newToken = await authService.refreshToken();
} catch {
  // Redirigir a login
  window.location.href = "/login";
}

// Opción 3: Verificar que el endpoint requiera auth
// En EJEMPLOS_CODIGO.md busca "[Authorize]"
// Si tiene [Authorize], entonces sí necesita token
```

---

## 🆘 Problema: CORS Error

### ❌ Síntoma

```
Access to XMLHttpRequest at 'https://api.okla.com.do/api/vehicles'
from origin 'https://okla.com.do' has been blocked by CORS policy
```

### 🔍 Diagnóstico

1. **Verifica CORS en Gateway**

   ```csharp
   // backend/Gateway/Gateway.Api/Program.cs

   builder.Services.AddCors(options =>
   {
       options.AddPolicy("AllowFrontend", policy =>
       {
           policy
               .WithOrigins("https://okla.com.do", "http://localhost:3000")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
       });
   });

   // Debe tener: app.UseCors("AllowFrontend");
   ```

2. **Verifica si es preflight request**

   ```
   OPTIONS /api/vehicles
   Debe retornar: 204 No Content
   ```

3. **Verifica headers en response**
   ```
   Access-Control-Allow-Origin: https://okla.com.do
   Access-Control-Allow-Methods: GET, POST, PUT, DELETE
   Access-Control-Allow-Headers: Content-Type, Authorization
   ```

### ✅ Solución

```bash
# Test CORS localmente
curl -i -X OPTIONS http://localhost:18443/api/vehicles \
  -H "Origin: http://localhost:3000" \
  -H "Access-Control-Request-Method: GET"

# Debe retornar headers Access-Control-*
```

Si ves que faltan headers:

```csharp
// Actualiza ocelot.prod.json y reinicia gateway
kubectl delete configmap gateway-config -n okla
kubectl create configmap gateway-config \
  --from-file=ocelot.json=backend/Gateway/Gateway.Api/ocelot.prod.json \
  -n okla
kubectl rollout restart deployment/gateway -n okla
```

---

## 🆘 Problema: 500 Internal Server Error

### ❌ Síntoma

```json
{
  "statusCode": 500,
  "message": "Internal Server Error"
}
```

### 🔍 Diagnóstico

1. **Ver logs del servicio**

   ```bash
   # Opción 1: Si está en Docker local
   docker-compose logs vehiclessaleservice | tail -100

   # Opción 2: Si está en Kubernetes
   kubectl logs deployment/vehiclessaleservice -n okla --tail=100
   ```

2. **Posibles causas**
   - ❌ Database connection error
   - ❌ Service no pudo conectar a otro servicio
   - ❌ Validation error no manejado
   - ❌ N+1 query problem (query muy lenta)
   - ❌ Out of memory

3. **Verificar database está UP**

   ```bash
   # Local
   docker-compose ps postgres

   # K8s
   kubectl get statefulset postgres -n okla
   ```

### ✅ Solución

```bash
# Check logs detallados
kubectl logs deployment/vehiclessaleservice -n okla -f

# Ver si hay errores de conexión
kubectl logs deployment/vehiclessaleservice -n okla | grep -i error

# Restart del servicio
kubectl rollout restart deployment/vehiclessaleservice -n okla

# Esperar a que reinicie
kubectl rollout status deployment/vehiclessaleservice -n okla
```

---

## 🆘 Problema: Network Timeout

### ❌ Síntoma

```
Error: timeout of 10000ms exceeded
```

### 🔍 Diagnóstico

1. **¿Es la red?**

   ```bash
   ping api.okla.com.do
   # Si no responde → problema de internet
   ```

2. **¿Es el servicio que está lento?**

   ```bash
   # Time the request
   time curl https://api.okla.com.do/health
   ```

3. **¿Es query muy compleja?**

   ```typescript
   // Búsqueda con muchos filtros puede ser lenta
   // Ver logs del servicio para queries lentas

   // Si tarda > 5 segundos, probablemente sea query
   ```

4. **¿Es falta de conexión?**
   ```bash
   # Verificar que servicio está corriendo
   kubectl get pods -n okla | grep vehiclessaleservice
   # Debe estar Running (1/1)
   ```

### ✅ Solución

```typescript
// Aumentar timeout en axiosConfig.ts
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  timeout: 30000, // De 10s a 30s
});

// O: Para requests específicos
await apiClient.get("/api/vehicles/search", {
  timeout: 60000, // 1 minuto para búsqueda compleja
});
```

```bash
# Optimizar query en backend si es muy lenta
# Ver: EXEMPLOS_CODIGO.md → QueryHandler → Use indexes
```

---

## 🆘 Problema: Token Expirado (401 Loop)

### ❌ Síntoma

```
Login → Redirect → 401 → Login nuevamente
(Loop infinito)
```

### 🔍 Diagnóstico

1. **Refresh token middleware**

   ```typescript
   // axiosConfig.ts debe tener:
   apiClient.interceptors.response.use(
     (response) => response,
     async (error) => {
       if (error.response?.status === 401) {
         // Intentar refresh token
         const newToken = await authService.refreshToken();
         // Retry original request
       }
     },
   );
   ```

2. **Refresh token expirado?**

   ```typescript
   // Si refresh token también expiró
   // No hay forma de recuperarse → Must login
   ```

3. **Endpoint no maneja refresh correctamente?**
   ```csharp
   // AuthService debe tener endpoint /refresh-token
   // Ver: EJEMPLOS_CODIGO.md → AuthController
   ```

### ✅ Solución

```typescript
// En axiosConfig.ts, mejora el error handling
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (
      error.response?.status === 401 &&
      !originalRequest._retry &&
      localStorage.getItem("refreshToken") // Solo si hay refresh token
    ) {
      originalRequest._retry = true;

      try {
        await authService.refreshToken();
        return apiClient(originalRequest); // Retry
      } catch (refreshError) {
        // Refresh falló → ir a login
        localStorage.removeItem("accessToken");
        localStorage.removeItem("refreshToken");
        window.location.href = "/login?redirect=" + window.location.pathname;
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  },
);
```

---

## 🆘 Problema: JWT Decoder Muestra Datos Incorrectos

### ❌ Síntoma

```
Token decodificado no muestra user ID correctamente
```

### 🔍 Diagnóstico

1. **Decodifica el token**

   ```javascript
   // En browser console
   const token = localStorage.getItem("accessToken");
   JSON.parse(atob(token.split(".")[1]));
   ```

2. **Busca estos fields**

   ```javascript
   {
     sub: "user-id",         // ← ID del usuario
     email: "user@example.com",
     role: "Individual",
     exp: 1705570000,        // ← Timestamp de expiración
     iat: 1705566400         // ← Issued at
   }
   ```

3. **¿El `sub` está vacío o es incorrecto?**
   - Backend no está generando el JWT correctamente

### ✅ Solución

```csharp
// En AuthService, verifica la generación del JWT
// backend/AuthService/AuthService.Api/Controllers/AuthController.cs

var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    // ↑ Esta línea crea el "sub" claim
    new Claim(ClaimTypes.Email, user.Email),
    new Claim("role", user.Role),
};

var token = _jwtTokenGenerator.GenerateToken(claims);
```

---

## 🆘 Problema: Imagen No Se Subió

### ❌ Síntoma

```
Upload button shows "Uploading..." pero nunca termina
POST /api/media/upload → no responde
```

### 🔍 Diagnóstico

1. **¿Es problema de tamaño?**

   ```typescript
   // MediaService probablemente tenga límite
   // Max: 5MB por imagen
   console.log(file.size / 1024 / 1024); // MB
   ```

2. **¿Es formato de imagen?**

   ```typescript
   // Soportados: JPEG, PNG
   // No soportados: WEBP, GIF, BMP

   if (!["image/jpeg", "image/png"].includes(file.type)) {
     // Mostrar error
   }
   ```

3. **¿MediaService está corriendo?**

   ```bash
   curl https://api.okla.com.do/health
   # Verificar que responda
   ```

4. **¿S3 está configurado?**
   - MediaService sube a AWS S3
   - Si S3 no está accesible → falla upload

### ✅ Solución

```typescript
// En ImageUpload.tsx, mejorar validaciones
const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
  const files = Array.from(e.target.files || []);

  // Validar tamaño
  const maxSize = 5 * 1024 * 1024; // 5MB
  const validFiles = files.filter((f) => {
    if (f.size > maxSize) {
      alert(`${f.name} es muy grande (máx 5MB)`);
      return false;
    }
    return true;
  });

  // Validar formato
  const validFormats = ["image/jpeg", "image/png"];
  const formatFiles = validFiles.filter((f) => {
    if (!validFormats.includes(f.type)) {
      alert(`${f.name} formato no soportado (JPEG o PNG)`);
      return false;
    }
    return true;
  });

  setSelectedFiles(formatFiles);
};

// Mejorar error handling
const handleUpload = async () => {
  try {
    const images = await mediaService.uploadImages(formData, (progress) => {
      setProgress((progress.loaded / progress.total) * 100);
    });
  } catch (error) {
    if (error.response?.status === 413) {
      alert("Archivo muy grande");
    } else if (error.response?.status === 400) {
      alert("Formato de archivo no válido");
    } else {
      alert("Error al subir: " + error.message);
    }
  }
};
```

---

## 🆘 Problema: "Cannot GET /api/admin/..."

### ❌ Síntoma

```
Admin accede a /api/admin/vehicles
Retorna 404
```

### 🔍 Diagnóstico

1. **¿AdminService está corriendo?**

   ```bash
   kubectl get pods -n okla | grep admin
   # Debe estar Running
   ```

2. **¿AdminService está en Gateway?**

   ```bash
   kubectl get configmap gateway-config -n okla -o yaml | grep admin
   ```

3. **¿Tienes permisos de Admin?**
   ```typescript
   // Verificar user role
   const user = useAuthStore((s) => s.user);
   console.log(user.role); // Debe ser "Admin"
   ```

### ✅ Solución

```bash
# Verificar ruta en Gateway
kubectl get configmap gateway-config -n okla -o yaml

# Si no está, agregar a ocelot.prod.json
{
  "DownstreamPathTemplate": "/api/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [
    { "Host": "adminservice", "Port": 8080 }
  ],
  "UpstreamPathTemplate": "/api/admin/{everything}",
  "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ]
}

# Luego actualizar configmap
kubectl delete configmap gateway-config -n okla
kubectl create configmap gateway-config \
  --from-file=ocelot.json=backend/Gateway/Gateway.Api/ocelot.prod.json \
  -n okla
kubectl rollout restart deployment/gateway -n okla
```

---

## 🆘 Problema: "Unauthorized" en Admin Endpoint

### ❌ Síntoma

```
Usuario es Admin pero retorna 401
```

### 🔍 Diagnóstico

1. **Verificar claims del JWT**

   ```javascript
   JSON.parse(atob(token.split(".")[1]));
   // ¿Tiene "role: Admin"?
   ```

2. **Verificar que el endpoint requiere Admin**

   ```csharp
   // En AdminController debe tener:
   [Authorize(Roles = "Admin")]
   ```

3. **¿Claims están configurados correctamente?**
   ```csharp
   // Al generar JWT en AuthService
   var claims = new List<Claim>
   {
       new Claim(ClaimType.Role, user.Role), // ← Importante
   };
   ```

### ✅ Solución

```csharp
// Verificar AuthService.Api/Controllers/AuthController.cs
public async Task<ActionResult<LoginResponse>> Login(LoginCommand command)
{
    var result = await _mediator.Send(command);

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, result.UserId),
        new Claim(ClaimTypes.Email, result.Email),
        new Claim(ClaimTypes.Role, result.Role), // ← Asegurar que está
    };

    var token = _jwtTokenGenerator.GenerateToken(claims);
    // ...
}
```

---

## 🆘 Problema: Feature completamente no funciona

### 📋 Checklist Debugging

```
¿Funciona en dev local?
├─ NO → Problema en tu código
│   └─ Ejecutar con debugger
│
└─ SÍ → Problema en producción
    ├─ ¿Servicio está en K8s?
    │ └─ kubectl get pods -n okla
    │
    ├─ ¿Servicio está en Gateway?
    │ └─ kubectl get configmap gateway-config -n okla -o yaml
    │
    ├─ ¿Logs muestran errores?
    │ └─ kubectl logs deployment/servicename -n okla
    │
    └─ ¿Database está disponible?
        └─ kubectl exec -it postgres-0 -n okla psql
```

---

## 📞 Cuando Nada Funciona

### 🆘 Emergency Contact

1. **Servicio no responde**

   ```bash
   kubectl rollout restart deployment/{servicename} -n okla
   kubectl rollout status deployment/{servicename} -n okla
   ```

2. **Gateway roto**

   ```bash
   # Reiniciar gateway
   kubectl rollout restart deployment/gateway -n okla

   # Ver si tiene config válido
   kubectl get configmap gateway-config -n okla -o yaml | head -50
   ```

3. **Database down**

   ```bash
   # Ver estado
   kubectl get statefulset postgres -n okla

   # Ver logs
   kubectl logs statefulset/postgres -n okla --tail=50
   ```

4. **Contactar Team**
   - Frontend: gmoreno@okla.com.do
   - Backend: backend@okla.com.do
   - DevOps: devops@okla.com.do

---

## 🎯 Resumen Rápido

| Problema           | Causa Probable       | Solución Rápida                |
| ------------------ | -------------------- | ------------------------------ |
| 404                | Ruta incorrecta      | Ver MICROSERVICIOS_GUIA_RAPIDA |
| 401                | Sin token o expirado | Hacer login / refresh token    |
| 403                | Sin permisos         | Verificar rol del usuario      |
| 500                | Error en servidor    | Ver logs del servicio          |
| CORS error         | Config CORS          | Reiniciar Gateway              |
| Timeout            | Servicio lento       | Aumentar timeout               |
| Upload no funciona | Tamaño/formato       | Validar archivo                |
| Token inválido     | Generación JWT       | Verificar claims en token      |

---

**🐛 Troubleshooting Guide - OKLA Marketplace**  
Enero 2026
