# 🧪 Sprint 1 - API Testing Documentation

**Fecha:** Enero 8, 2026  
**Estado:** Testing Manual Requerido  
**Servicios:** MaintenanceService, ComparisonService, AlertService

---

## 📋 Resumen de Testing

### ✅ Backend - Build Testing (COMPLETADO)

Todos los servicios compilan correctamente con Docker:

```bash
# MaintenanceService
docker build -t cardealer-maintenanceservice:test backend/MaintenanceService/MaintenanceService.Api
# ✅ SUCCESS

# ComparisonService
docker build -t cardealer-comparisonservice:test backend/ComparisonService/ComparisonService.Api
# ✅ SUCCESS

# AlertService
docker build -t cardealer-alertservice:test backend/AlertService/AlertService.Api
# ✅ SUCCESS
```

### ⚠️ API Endpoints - Requiere Testing Manual con Servicios en Producción

Los siguientes endpoints necesitan ser probados en el ambiente de producción (DOKS) o desarrollo (docker-compose):

---

## 🛠️ MaintenanceService - Endpoints

**Base URL:** `https://api.okla.com.do/api/maintenance` (Producción)  
**Local:** `http://localhost:5061/api/maintenance` (Desarrollo)

### 1. Get Current Maintenance

Obtiene la ventana de mantenimiento activa actual.

```bash
curl -X GET "https://api.okla.com.do/api/maintenance/current" \
  -H "Content-Type: application/json"
```

**Respuesta Esperada (200 OK):**

```json
{
  "isActive": true,
  "message": "Mantenimiento programado del sistema",
  "startTime": "2026-01-10T02:00:00Z",
  "endTime": "2026-01-10T04:00:00Z",
  "severity": "warning",
  "affectedServices": ["VehiclesSaleService", "MediaService"]
}
```

**Respuesta Sin Mantenimiento (200 OK):**

```json
{
  "isActive": false
}
```

### 2. Get All Maintenance Windows

Obtiene todas las ventanas de mantenimiento (requiere Admin).

```bash
curl -X GET "https://api.okla.com.do/api/maintenance" \
  -H "Authorization: Bearer {ADMIN_TOKEN}" \
  -H "Content-Type: application/json"
```

**Respuesta Esperada (200 OK):**

```json
[
  {
    "id": "uuid",
    "title": "Actualización de base de datos",
    "description": "Migración de esquema PostgreSQL",
    "startTime": "2026-01-10T02:00:00Z",
    "endTime": "2026-01-10T04:00:00Z",
    "maintenanceType": "Scheduled",
    "severity": "warning",
    "isActive": true,
    "affectedServices": ["VehiclesSaleService"],
    "createdBy": "admin@okla.com.do"
  }
]
```

### 3. Create Maintenance Window

Crea una nueva ventana de mantenimiento (requiere Admin).

```bash
curl -X POST "https://api.okla.com.do/api/maintenance" \
  -H "Authorization: Bearer {ADMIN_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Mantenimiento de emergencia",
    "description": "Actualización crítica de seguridad",
    "startTime": "2026-01-15T03:00:00Z",
    "endTime": "2026-01-15T05:00:00Z",
    "maintenanceType": "Emergency",
    "severity": "error",
    "affectedServices": ["All"],
    "notifyUsers": true
  }'
```

**Respuesta Esperada (201 Created):**

```json
{
  "id": "new-uuid",
  "title": "Mantenimiento de emergencia",
  "isActive": false,
  "createdAt": "2026-01-08T10:30:00Z"
}
```

### 4. Update Maintenance Window

Actualiza una ventana de mantenimiento existente (requiere Admin).

```bash
curl -X PUT "https://api.okla.com.do/api/maintenance/{id}" \
  -H "Authorization: Bearer {ADMIN_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Mantenimiento extendido",
    "endTime": "2026-01-15T06:00:00Z"
  }'
```

### 5. Delete Maintenance Window

Elimina una ventana de mantenimiento (requiere Admin).

```bash
curl -X DELETE "https://api.okla.com.do/api/maintenance/{id}" \
  -H "Authorization: Bearer {ADMIN_TOKEN}"
```

**Respuesta Esperada (204 No Content)**

### 6. Health Check

```bash
curl -X GET "https://api.okla.com.do/api/maintenance/health"
```

**Respuesta Esperada (200 OK):**

```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "rabbitmq": "Healthy"
  }
}
```

---

## 🔄 ComparisonService - Endpoints

**Base URL:** `https://api.okla.com.do/api/comparisons` (Producción)  
**Local:** `http://localhost:5066/api/comparisons` (Desarrollo)

### 1. Get User's Comparisons

Obtiene todas las comparaciones del usuario autenticado.

```bash
curl -X GET "https://api.okla.com.do/api/comparisons" \
  -H "Authorization: Bearer {USER_TOKEN}" \
  -H "Content-Type: application/json"
```

**Respuesta Esperada (200 OK):**

```json
[
  {
    "id": "comparison-uuid",
    "vehicles": [
      {
        "id": "vehicle-1-uuid",
        "make": "Toyota",
        "model": "Corolla",
        "year": 2023,
        "price": 2500000,
        "mileage": 15000,
        "transmission": "Automática",
        "fuelType": "Gasolina",
        "engineSize": "1.8L",
        "horsepower": 140,
        "imageUrl": "https://..."
      },
      {
        "id": "vehicle-2-uuid",
        "make": "Honda",
        "model": "Civic",
        "year": 2023,
        "price": 2800000,
        "mileage": 12000,
        "transmission": "Automática",
        "fuelType": "Gasolina",
        "engineSize": "2.0L",
        "horsepower": 158,
        "imageUrl": "https://..."
      }
    ],
    "createdAt": "2026-01-08T10:00:00Z"
  }
]
```

### 2. Create Comparison

Crea una nueva comparación (máximo 3 vehículos).

```bash
curl -X POST "https://api.okla.com.do/api/comparisons" \
  -H "Authorization: Bearer {USER_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "vehicleIds": [
      "vehicle-1-uuid",
      "vehicle-2-uuid"
    ]
  }'
```

**Respuesta Esperada (201 Created):**

```json
{
  "id": "new-comparison-uuid",
  "vehicleCount": 2,
  "createdAt": "2026-01-08T10:30:00Z"
}
```

**Validaciones:**

- Máximo 3 vehículos por comparación (400 Bad Request si excede)
- Vehículos deben existir (404 Not Found si no existen)
- Usuario debe estar autenticado (401 Unauthorized)

### 3. Add Vehicle to Comparison

Agrega un vehículo a una comparación existente.

```bash
curl -X POST "https://api.okla.com.do/api/comparisons/{comparisonId}/vehicles/{vehicleId}" \
  -H "Authorization: Bearer {USER_TOKEN}" \
  -H "Content-Type: application/json"
```

**Respuesta Esperada (200 OK):**

```json
{
  "id": "comparison-uuid",
  "vehicleCount": 3,
  "message": "Vehicle added successfully"
}
```

**Errores:**

- 400 Bad Request: "Maximum 3 vehicles per comparison"
- 404 Not Found: "Comparison not found" o "Vehicle not found"
- 409 Conflict: "Vehicle already in comparison"

### 4. Remove Vehicle from Comparison

Elimina un vehículo de una comparación.

```bash
curl -X DELETE "https://api.okla.com.do/api/comparisons/{comparisonId}/vehicles/{vehicleId}" \
  -H "Authorization: Bearer {USER_TOKEN}"
```

**Respuesta Esperada (204 No Content)**

### 5. Share Comparison

Genera un link público para compartir una comparación.

```bash
curl -X POST "https://api.okla.com.do/api/comparisons/{comparisonId}/share" \
  -H "Authorization: Bearer {USER_TOKEN}" \
  -H "Content-Type: application/json"
```

**Respuesta Esperada (200 OK):**

```json
{
  "shareUrl": "https://okla.com.do/compare/abc123xyz",
  "shareToken": "abc123xyz",
  "expiresAt": "2026-02-08T10:30:00Z"
}
```

### 6. Delete Comparison

Elimina una comparación completa.

```bash
curl -X DELETE "https://api.okla.com.do/api/comparisons/{comparisonId}" \
  -H "Authorization: Bearer {USER_TOKEN}"
```

**Respuesta Esperada (204 No Content)**

### 7. Health Check

```bash
curl -X GET "https://api.okla.com.do/api/comparisons/health"
```

---

## 🔔 AlertService - Endpoints

**Base URL:** `https://api.okla.com.do/api/alerts` (Producción)  
**Local:** `http://localhost:5067/api/alerts` (Desarrollo)

### 1. Get Price Alerts

Obtiene todas las alertas de precio del usuario.

```bash
curl -X GET "https://api.okla.com.do/api/alerts/price-alerts" \
  -H "Authorization: Bearer {USER_TOKEN}" \
  -H "Content-Type: application/json"
```

**Respuesta Esperada (200 OK):**

```json
[
  {
    "id": "alert-uuid",
    "vehicleId": "vehicle-uuid",
    "vehicleTitle": "Toyota Corolla 2023",
    "currentPrice": 2500000,
    "targetPrice": 2200000,
    "isActive": true,
    "createdAt": "2026-01-08T10:00:00Z",
    "triggeredAt": null
  }
]
```

### 2. Create Price Alert

Crea una alerta de precio para un vehículo.

```bash
curl -X POST "https://api.okla.com.do/api/alerts/price-alerts" \
  -H "Authorization: Bearer {USER_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "vehicleId": "vehicle-uuid",
    "targetPrice": 2200000,
    "notifyEmail": true,
    "notifySMS": false
  }'
```

**Respuesta Esperada (201 Created):**

```json
{
  "id": "new-alert-uuid",
  "vehicleId": "vehicle-uuid",
  "targetPrice": 2200000,
  "isActive": true,
  "createdAt": "2026-01-08T10:30:00Z"
}
```

**Validaciones:**

- Target price debe ser menor que precio actual (400 Bad Request)
- Vehículo debe existir (404 Not Found)
- Usuario solo puede tener 1 alerta por vehículo (409 Conflict)

### 3. Update Price Alert

Actualiza el precio objetivo de una alerta.

```bash
curl -X PUT "https://api.okla.com.do/api/alerts/price-alerts/{alertId}" \
  -H "Authorization: Bearer {USER_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "targetPrice": 2000000
  }'
```

**Respuesta Esperada (200 OK):**

```json
{
  "id": "alert-uuid",
  "targetPrice": 2000000,
  "updatedAt": "2026-01-08T11:00:00Z"
}
```

### 4. Toggle Price Alert (Activate/Pause)

Activa o pausa una alerta de precio.

```bash
curl -X PUT "https://api.okla.com.do/api/alerts/price-alerts/{alertId}/toggle" \
  -H "Authorization: Bearer {USER_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "isActive": false
  }'
```

**Respuesta Esperada (200 OK):**

```json
{
  "id": "alert-uuid",
  "isActive": false
}
```

### 5. Delete Price Alert

Elimina una alerta de precio.

```bash
curl -X DELETE "https://api.okla.com.do/api/alerts/price-alerts/{alertId}" \
  -H "Authorization: Bearer {USER_TOKEN}"
```

**Respuesta Esperada (204 No Content)**

### 6. Get Saved Searches

Obtiene todas las búsquedas guardadas del usuario.

```bash
curl -X GET "https://api.okla.com.do/api/alerts/saved-searches" \
  -H "Authorization: Bearer {USER_TOKEN}" \
  -H "Content-Type: application/json"
```

**Respuesta Esperada (200 OK):**

```json
[
  {
    "id": "search-uuid",
    "name": "Toyota 2020-2024",
    "filters": {
      "make": "Toyota",
      "minYear": 2020,
      "maxYear": 2024,
      "minPrice": 1500000,
      "maxPrice": 3000000
    },
    "isActive": true,
    "createdAt": "2026-01-08T10:00:00Z",
    "lastNotified": null
  }
]
```

### 7. Create Saved Search

Guarda una búsqueda con filtros para recibir notificaciones de nuevos vehículos.

```bash
curl -X POST "https://api.okla.com.do/api/alerts/saved-searches" \
  -H "Authorization: Bearer {USER_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Honda Sedanes Recientes",
    "filters": {
      "make": "Honda",
      "bodyType": "Sedan",
      "minYear": 2022,
      "maxPrice": 3500000
    },
    "notifyEmail": true,
    "notifyFrequency": "daily"
  }'
```

**Respuesta Esperada (201 Created):**

```json
{
  "id": "new-search-uuid",
  "name": "Honda Sedanes Recientes",
  "isActive": true,
  "createdAt": "2026-01-08T10:30:00Z"
}
```

**Validaciones:**

- Nombre requerido (400 Bad Request)
- Al menos 1 filtro requerido (400 Bad Request)
- Usuario puede tener máximo 10 búsquedas guardadas (409 Conflict)

### 8. Update Saved Search

Actualiza los filtros de una búsqueda guardada.

```bash
curl -X PUT "https://api.okla.com.do/api/alerts/saved-searches/{searchId}" \
  -H "Authorization: Bearer {USER_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Honda Sedanes 2023+",
    "filters": {
      "minYear": 2023
    }
  }'
```

### 9. Toggle Saved Search

Activa o pausa una búsqueda guardada.

```bash
curl -X PUT "https://api.okla.com.do/api/alerts/saved-searches/{searchId}/toggle" \
  -H "Authorization: Bearer {USER_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "isActive": false
  }'
```

### 10. Delete Saved Search

Elimina una búsqueda guardada.

```bash
curl -X DELETE "https://api.okla.com.do/api/alerts/saved-searches/{searchId}" \
  -H "Authorization: Bearer {USER_TOKEN}"
```

**Respuesta Esperada (204 No Content)**

### 11. Get Free Days Left (Early Bird)

Obtiene los días restantes de periodo gratuito para Early Bird users.

```bash
curl -X GET "https://api.okla.com.do/api/alerts/free-days-left" \
  -H "Authorization: Bearer {USER_TOKEN}" \
  -H "Content-Type: application/json"
```

**Respuesta Esperada (200 OK):**

```json
{
  "daysLeft": 45,
  "isEarlyBird": true,
  "enrolledAt": "2026-01-08T00:00:00Z",
  "expiresAt": "2026-04-30T23:59:59Z"
}
```

**Para usuarios NO Early Bird:**

```json
{
  "daysLeft": 0,
  "isEarlyBird": false
}
```

### 12. Health Check

```bash
curl -X GET "https://api.okla.com.do/api/alerts/health"
```

---

## 🧪 Testing Checklist

### Pre-requisitos

- [ ] Servicios desplegados en DOKS
- [ ] Gateway configurado con rutas correctas
- [ ] PostgreSQL con migraciones aplicadas
- [ ] Token JWT válido para testing

### MaintenanceService

- [ ] GET /api/maintenance/current (sin mantenimiento activo)
- [ ] GET /api/maintenance/current (con mantenimiento activo)
- [ ] POST /api/maintenance (crear ventana, requiere admin)
- [ ] GET /api/maintenance (listar todas, requiere admin)
- [ ] PUT /api/maintenance/{id} (actualizar, requiere admin)
- [ ] DELETE /api/maintenance/{id} (eliminar, requiere admin)
- [ ] GET /api/maintenance/health

### ComparisonService

- [ ] GET /api/comparisons (sin comparaciones)
- [ ] POST /api/comparisons (crear nueva con 2 vehículos)
- [ ] POST /api/comparisons/{id}/vehicles/{vehicleId} (agregar 3er vehículo)
- [ ] POST /api/comparisons/{id}/vehicles/{vehicleId} (error: máximo 3)
- [ ] DELETE /api/comparisons/{id}/vehicles/{vehicleId} (remover vehículo)
- [ ] POST /api/comparisons/{id}/share (generar link)
- [ ] DELETE /api/comparisons/{id} (eliminar comparación)
- [ ] GET /api/comparisons/health

### AlertService

- [ ] GET /api/alerts/price-alerts (sin alertas)
- [ ] POST /api/alerts/price-alerts (crear alerta)
- [ ] PUT /api/alerts/price-alerts/{id} (actualizar precio objetivo)
- [ ] PUT /api/alerts/price-alerts/{id}/toggle (pausar/activar)
- [ ] DELETE /api/alerts/price-alerts/{id} (eliminar)
- [ ] GET /api/alerts/saved-searches (sin búsquedas)
- [ ] POST /api/alerts/saved-searches (crear búsqueda)
- [ ] PUT /api/alerts/saved-searches/{id} (actualizar filtros)
- [ ] PUT /api/alerts/saved-searches/{id}/toggle (pausar/activar)
- [ ] DELETE /api/alerts/saved-searches/{id} (eliminar)
- [ ] GET /api/alerts/free-days-left (Early Bird user)
- [ ] GET /api/alerts/free-days-left (Regular user)
- [ ] GET /api/alerts/health

---

## 🚀 Testing en Producción (DOKS)

### Paso 1: Verificar Gateway

```bash
# Health check general
curl https://api.okla.com.do/health

# Verificar que servicios están registrados
kubectl get pods -n okla | grep -E "(maintenance|comparison|alert)"
```

### Paso 2: Obtener Token JWT

```bash
# Login para obtener token
curl -X POST "https://api.okla.com.do/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@okla.com.do",
    "password": "TestPassword123!"
  }'

# Guardar token en variable
export TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### Paso 3: Probar Endpoints (Ejemplo)

```bash
# MaintenanceService: Current maintenance
curl -s "https://api.okla.com.do/api/maintenance/current" | jq

# ComparisonService: Get comparisons
curl -s "https://api.okla.com.do/api/comparisons" \
  -H "Authorization: Bearer $TOKEN" | jq

# AlertService: Get price alerts
curl -s "https://api.okla.com.do/api/alerts/price-alerts" \
  -H "Authorization: Bearer $TOKEN" | jq

# AlertService: Get free days left
curl -s "https://api.okla.com.do/api/alerts/free-days-left" \
  -H "Authorization: Bearer $TOKEN" | jq
```

---

## 📊 Resultados Esperados

### Status Codes

| Código | Significado           | Uso                         |
| ------ | --------------------- | --------------------------- |
| 200    | OK                    | GET/PUT exitoso             |
| 201    | Created               | POST exitoso                |
| 204    | No Content            | DELETE exitoso              |
| 400    | Bad Request           | Validación falló            |
| 401    | Unauthorized          | Token inválido o faltante   |
| 403    | Forbidden             | Sin permisos (ej: no admin) |
| 404    | Not Found             | Recurso no existe           |
| 409    | Conflict              | Duplicado o límite excedido |
| 500    | Internal Server Error | Error del servidor          |

### Response Headers

Todos los endpoints deben retornar:

```
Content-Type: application/json
X-Request-Id: {uuid}
X-Service-Name: {ServiceName}
```

---

## 🔧 Troubleshooting

### Error: "Service unavailable"

```bash
# Verificar que el pod está corriendo
kubectl get pods -n okla | grep {servicename}

# Ver logs del servicio
kubectl logs -f deployment/{servicename} -n okla
```

### Error: "Unauthorized"

```bash
# Verificar token JWT
echo $TOKEN | cut -d'.' -f2 | base64 -d | jq

# Obtener nuevo token
curl -X POST "https://api.okla.com.do/api/auth/login" ...
```

### Error: "Database connection failed"

```bash
# Verificar PostgreSQL
kubectl exec -it postgres-0 -n okla -- psql -U postgres -c "\l"

# Verificar conexión desde el pod
kubectl exec -it deployment/{servicename} -n okla -- \
  curl http://postgres:5432
```

---

## ✅ Conclusión

**Backend Compilation:** ✅ PASS  
**Frontend Build:** ✅ PASS  
**API Endpoints Testing:** ⚠️ PENDING (Requiere servicios en producción)

**Próximos Pasos:**

1. Deploy de servicios a DOKS (si no están desplegados)
2. Ejecutar testing manual de cada endpoint según este documento
3. Verificar respuestas JSON y status codes
4. Documentar cualquier bug encontrado
5. Crear tests automatizados (integration tests)

---

**Documento creado:** Enero 8, 2026  
**Autor:** GitHub Copilot + Gregory Moreno  
**Versión:** 1.0
