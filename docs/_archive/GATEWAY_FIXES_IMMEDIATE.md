# 🔧 CORRECCIONES INMEDIATAS PARA GATEWAY

**Fecha:** 29 de Enero, 2026  
**Archivo a Modificar:** `backend/Gateway/Gateway.Api/ocelot.prod.json`

---

## 🚨 PROBLEMA CRÍTICO DETECTADO

El frontend de OKLA está haciendo llamadas a endpoints que **NO ESTÁN REGISTRADOS** en el Gateway:

### Endpoints Faltantes que Rompen la Aplicación

1. **MaintenanceService** - El componente `MaintenanceBanner` llama a `/api/maintenance/current`
2. **AlertService** - Las páginas `AlertsPage` y `FavoritesPage` hacen múltiples llamadas

---

## 📝 CONFIGURACIONES A AGREGAR

### 1. MaintenanceService ⚠️ PRIORIDAD ALTA

**Agregar ANTES de la línea de `GlobalConfiguration`:**

```json
{
  "UpstreamPathTemplate": "/api/maintenance/health",
  "UpstreamHttpMethod": ["GET"],
  "DownstreamPathTemplate": "/health",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "maintenanceservice", "Port": 8080 }]
},
{
  "UpstreamPathTemplate": "/api/maintenance/status",
  "UpstreamHttpMethod": ["GET"],
  "DownstreamPathTemplate": "/api/maintenance/status",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "maintenanceservice", "Port": 8080 }],
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
{
  "UpstreamPathTemplate": "/api/maintenance/current",
  "UpstreamHttpMethod": ["GET"],
  "DownstreamPathTemplate": "/api/maintenance/current",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "maintenanceservice", "Port": 8080 }],
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
{
  "UpstreamPathTemplate": "/api/maintenance/upcoming",
  "UpstreamHttpMethod": ["GET"],
  "DownstreamPathTemplate": "/api/maintenance/upcoming",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "maintenanceservice", "Port": 8080 }],
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
{
  "UpstreamPathTemplate": "/api/maintenance/{everything}",
  "UpstreamHttpMethod": ["OPTIONS", "GET", "POST", "PUT", "DELETE"],
  "DownstreamPathTemplate": "/api/maintenance/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "maintenanceservice", "Port": 8080 }],
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
```

**Razón:** El `MaintenanceBanner.tsx` hace llamada a `/api/maintenance/current` para mostrar avisos de mantenimiento programado.

---

### 2. AlertService ⚠️ PRIORIDAD ALTA

**Agregar DESPUÉS de MaintenanceService:**

```json
{
  "UpstreamPathTemplate": "/api/savedsearches/health",
  "UpstreamHttpMethod": ["GET"],
  "DownstreamPathTemplate": "/health",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "alertservice", "Port": 8080 }]
},
{
  "UpstreamPathTemplate": "/api/savedsearches",
  "UpstreamHttpMethod": ["OPTIONS", "GET", "POST"],
  "DownstreamPathTemplate": "/api/savedsearches",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "alertservice", "Port": 8080 }],
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
{
  "UpstreamPathTemplate": "/api/savedsearches/{id}",
  "UpstreamHttpMethod": ["OPTIONS", "GET", "PUT", "DELETE"],
  "DownstreamPathTemplate": "/api/savedsearches/{id}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "alertservice", "Port": 8080 }],
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
{
  "UpstreamPathTemplate": "/api/savedsearches/{id}/name",
  "UpstreamHttpMethod": ["PUT"],
  "DownstreamPathTemplate": "/api/savedsearches/{id}/name",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "alertservice", "Port": 8080 }],
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
{
  "UpstreamPathTemplate": "/api/savedsearches/{id}/criteria",
  "UpstreamHttpMethod": ["PUT"],
  "DownstreamPathTemplate": "/api/savedsearches/{id}/criteria",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "alertservice", "Port": 8080 }],
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
{
  "UpstreamPathTemplate": "/api/savedsearches/{id}/notifications",
  "UpstreamHttpMethod": ["PUT"],
  "DownstreamPathTemplate": "/api/savedsearches/{id}/notifications",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "alertservice", "Port": 8080 }],
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
{
  "UpstreamPathTemplate": "/api/savedsearches/{id}/activate",
  "UpstreamHttpMethod": ["POST"],
  "DownstreamPathTemplate": "/api/savedsearches/{id}/activate",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "alertservice", "Port": 8080 }],
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
{
  "UpstreamPathTemplate": "/api/savedsearches/{id}/deactivate",
  "UpstreamHttpMethod": ["POST"],
  "DownstreamPathTemplate": "/api/savedsearches/{id}/deactivate",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "alertservice", "Port": 8080 }],
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
{
  "UpstreamPathTemplate": "/api/pricealerts/health",
  "UpstreamHttpMethod": ["GET"],
  "DownstreamPathTemplate": "/health",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "alertservice", "Port": 8080 }]
},
{
  "UpstreamPathTemplate": "/api/pricealerts",
  "UpstreamHttpMethod": ["OPTIONS", "GET", "POST"],
  "DownstreamPathTemplate": "/api/pricealerts",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "alertservice", "Port": 8080 }],
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
{
  "UpstreamPathTemplate": "/api/pricealerts/{id}",
  "UpstreamHttpMethod": ["OPTIONS", "GET", "DELETE"],
  "DownstreamPathTemplate": "/api/pricealerts/{id}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "alertservice", "Port": 8080 }],
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
{
  "UpstreamPathTemplate": "/api/pricealerts/{id}/target-price",
  "UpstreamHttpMethod": ["PUT"],
  "DownstreamPathTemplate": "/api/pricealerts/{id}/target-price",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "alertservice", "Port": 8080 }],
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
{
  "UpstreamPathTemplate": "/api/pricealerts/{id}/activate",
  "UpstreamHttpMethod": ["POST"],
  "DownstreamPathTemplate": "/api/pricealerts/{id}/activate",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "alertservice", "Port": 8080 }],
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
{
  "UpstreamPathTemplate": "/api/pricealerts/{id}/deactivate",
  "UpstreamHttpMethod": ["POST"],
  "DownstreamPathTemplate": "/api/pricealerts/{id}/deactivate",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "alertservice", "Port": 8080 }],
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
{
  "UpstreamPathTemplate": "/api/pricealerts/{id}/reset",
  "UpstreamHttpMethod": ["POST"],
  "DownstreamPathTemplate": "/api/pricealerts/{id}/reset",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "alertservice", "Port": 8080 }],
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
```

**Razón:** `AlertsPage.tsx` y `FavoritesPage.tsx` dependen de estos endpoints.

---

### 3. DealerAnalyticsService 🟡 PRIORIDAD MEDIA

```json
{
  "UpstreamPathTemplate": "/api/dealer-analytics/health",
  "UpstreamHttpMethod": ["GET"],
  "DownstreamPathTemplate": "/health",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "dealeranalyticsservice", "Port": 8080 }]
},
{
  "UpstreamPathTemplate": "/api/dealer-analytics/{everything}",
  "UpstreamHttpMethod": ["OPTIONS", "GET"],
  "DownstreamPathTemplate": "/api/dealer-analytics/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "dealeranalyticsservice", "Port": 8080 }],
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
{
  "UpstreamPathTemplate": "/api/analytics/{everything}",
  "UpstreamHttpMethod": ["OPTIONS", "GET"],
  "DownstreamPathTemplate": "/api/analytics/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "dealeranalyticsservice", "Port": 8080 }],
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
```

---

### 4. EventTrackingService 🟡 PRIORIDAD MEDIA

```json
{
  "UpstreamPathTemplate": "/api/events/health",
  "UpstreamHttpMethod": ["GET"],
  "DownstreamPathTemplate": "/health",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "eventtrackingservice", "Port": 8080 }]
},
{
  "UpstreamPathTemplate": "/api/events/{everything}",
  "UpstreamHttpMethod": ["OPTIONS", "GET", "POST"],
  "DownstreamPathTemplate": "/api/events/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "eventtrackingservice", "Port": 8080 }],
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
```

---

### 5. SpyneIntegrationService 🟡 PRIORIDAD MEDIA

```json
{
  "UpstreamPathTemplate": "/api/vehicle-images/health",
  "UpstreamHttpMethod": ["GET"],
  "DownstreamPathTemplate": "/health",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "spyneintegrationservice", "Port": 8080 }]
},
{
  "UpstreamPathTemplate": "/api/vehicle-images/{everything}",
  "UpstreamHttpMethod": ["OPTIONS", "GET", "POST", "PUT", "DELETE"],
  "DownstreamPathTemplate": "/api/vehicle-images/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "spyneintegrationservice", "Port": 8080 }],
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 120000
  }
},
```

---

### 6. KYCService 🟢 PRIORIDAD BAJA

```json
{
  "UpstreamPathTemplate": "/api/kyc/health",
  "UpstreamHttpMethod": ["GET"],
  "DownstreamPathTemplate": "/health",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "kycservice", "Port": 8080 }]
},
{
  "UpstreamPathTemplate": "/api/kyc/{everything}",
  "UpstreamHttpMethod": ["OPTIONS", "GET", "POST", "PUT", "DELETE"],
  "DownstreamPathTemplate": "/api/kyc/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "kycservice", "Port": 8080 }],
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
```

---

## 🔍 VERIFICACIONES A REALIZAR

### Problema: ComparisonService - Desincronización de Rutas

**Estado Actual en Gateway:**

```json
{
  "UpstreamPathTemplate": "/api/vehiclecomparisons/{everything}",
  "DownstreamPathTemplate": "/api/vehiclecomparisons/{everything}"
}
```

**Backend Controller:**

```csharp
[Route("api/comparisons")]  // ← Nota: SIN "vehicle" prefix
```

**Solución: Cambiar el DownstreamPathTemplate:**

```json
{
  "UpstreamPathTemplate": "/api/vehiclecomparisons/{everything}",
  "UpstreamHttpMethod": ["OPTIONS", "GET", "POST", "PUT", "DELETE"],
  "DownstreamPathTemplate": "/api/comparisons/{everything}", // ← CORREGIR ESTO
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "comparisonservice", "Port": 8080 }],
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
}
```

---

### Problema: AzulPaymentService - Desincronización de Rutas

**Estado Actual en Gateway:**

```json
{
  "UpstreamPathTemplate": "/api/azul-payment/{everything}",
  "DownstreamPathTemplate": "/api/azul-payment/{everything}"
}
```

**Backend Controller:**

```csharp
[Route("api/azul")]  // ← Nota: SIN "-payment" suffix
```

**Solución: Cambiar el DownstreamPathTemplate:**

```json
{
  "UpstreamPathTemplate": "/api/azul-payment/{everything}",
  "UpstreamHttpMethod": ["OPTIONS", "GET", "POST", "PUT", "DELETE"],
  "DownstreamPathTemplate": "/api/azul/{everything}", // ← CORREGIR ESTO
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "azulpaymentservice", "Port": 8080 }],
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
}
```

---

## 🛠️ INSTRUCCIONES DE APLICACIÓN

### Paso 1: Backup

```bash
cd backend/Gateway/Gateway.Api
cp ocelot.prod.json ocelot.prod.json.backup-$(date +%Y%m%d-%H%M%S)
```

### Paso 2: Editar ocelot.prod.json

Abrir el archivo y agregar las configuraciones en el orden indicado (antes de `GlobalConfiguration`).

### Paso 3: Validar JSON

```bash
# Verificar que el JSON es válido
cat ocelot.prod.json | python3 -m json.tool > /dev/null && echo "✅ JSON válido"
```

### Paso 4: Actualizar ConfigMap en Kubernetes

```bash
# Eliminar ConfigMap anterior
kubectl delete configmap gateway-config -n okla

# Crear nuevo ConfigMap
kubectl create configmap gateway-config \
  --from-file=ocelot.json=backend/Gateway/Gateway.Api/ocelot.prod.json \
  -n okla

# Reiniciar Gateway para aplicar cambios
kubectl rollout restart deployment/gateway -n okla
```

### Paso 5: Verificar Deployment

```bash
# Ver logs del Gateway
kubectl logs -f deployment/gateway -n okla

# Probar endpoints nuevos
curl https://api.okla.com.do/api/maintenance/current
curl -H "Authorization: Bearer YOUR_TOKEN" https://api.okla.com.do/api/pricealerts
```

---

## ⚠️ NOTAS IMPORTANTES

### Orden de Rutas en ocelot.json

**CRÍTICO:** Las rutas MÁS ESPECÍFICAS deben ir ANTES que las genéricas (`{everything}`).

**Ejemplo:**

```json
// ✅ CORRECTO
{ "UpstreamPathTemplate": "/api/maintenance/current" },     // Específica primero
{ "UpstreamPathTemplate": "/api/maintenance/{id}" },        // Específica
{ "UpstreamPathTemplate": "/api/maintenance/{everything}" } // Genérica al final

// ❌ INCORRECTO
{ "UpstreamPathTemplate": "/api/maintenance/{everything}" } // Genérica primero
{ "UpstreamPathTemplate": "/api/maintenance/current" }      // Nunca se alcanza
```

**Razón:** Ocelot hace matching en orden de aparición. Si pones `{everything}` primero, las rutas específicas nunca se ejecutarán.

---

### Testing Local con Docker Compose

Si estás probando en `docker-compose.yml` local, también actualiza `ocelot.dev.json`:

```bash
# Para desarrollo local
cd backend/Gateway/Gateway.Api
# Copiar cambios de ocelot.prod.json a ocelot.dev.json
# (Cambiar hosts de K8s a nombres de docker-compose si es necesario)
```

---

## 📊 IMPACTO ESPERADO

### Antes de Aplicar Correcciones

- ❌ `MaintenanceBanner` no funciona (404 en `/api/maintenance/current`)
- ❌ `AlertsPage` no funciona (404 en `/api/pricealerts`, `/api/savedsearches`)
- ❌ `DealerDashboard` analytics incompleto
- ⚠️ Errores 404 en logs del Gateway

### Después de Aplicar Correcciones

- ✅ `MaintenanceBanner` muestra avisos correctamente
- ✅ `AlertsPage` y `FavoritesPage` completamente funcionales
- ✅ Analytics de dealer disponibles
- ✅ Sin errores 404 en rutas de microservicios activos

---

## 🔄 WORKFLOW AUTOMATIZADO (FUTURO)

Crear script que detecte automáticamente endpoints faltantes:

```bash
#!/bin/bash
# scripts/check-gateway-coverage.sh

echo "🔍 Auditando cobertura del Gateway..."

# 1. Extraer todos los controllers de backend
find backend -name "*Controller.cs" | while read controller; do
  grep -o 'Route("api/[^"]*")' "$controller" | sed 's/Route("//;s/")//'
done | sort -u > /tmp/backend-routes.txt

# 2. Extraer rutas del Gateway
cat backend/Gateway/Gateway.Api/ocelot.prod.json | \
  grep UpstreamPathTemplate | \
  sed 's/.*"UpstreamPathTemplate": "//;s/".*//' | \
  sort -u > /tmp/gateway-routes.txt

# 3. Comparar y mostrar faltantes
echo "❌ Rutas faltantes en Gateway:"
comm -23 /tmp/backend-routes.txt /tmp/gateway-routes.txt
```

---

**Documento Creado:** 29 de Enero, 2026  
**Próxima Revisión:** Después de aplicar correcciones  
**Responsable:** DevOps / Backend Team
