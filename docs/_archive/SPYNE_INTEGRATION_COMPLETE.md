# 🎨 Spyne AI Integration - Configuración Completa

**Fecha:** Enero 20, 2026  
**Estado:** ✅ FUNCIONANDO EN DOCKER

---

## 📋 Resumen

La integración con Spyne AI está completamente configurada para transformar imágenes de vehículos con fondos profesionales usando IA.

### 🔑 Credenciales Configuradas

```
API Key: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJlbnRlcnByaXNlSWQiOiIwOTg3ODAwZjEiLCJ0ZWFtSWQiOiI5ZDJjMjVmNTQ2IiwidXNlcklkIjoiYTQ1ZGE3MWEiLCJzZWNyZXRLZXkiOiI2OTFlZWM3MjFiZDg0YzMwOWEwZTE0NzBiYjljZjIxYiIsImlhdCI6MTc2ODk1MzQ1OCwiZXhwIjoxODYzNTYxNDU4fQ.d08jh3rlDG7sE46b1wpVai5Hy5L3ZAW_iv4Xm81AiGk

Auth Type: Bearer Token (Authorization: Bearer {key})
Team ID: 9d2c25f546
Enterprise ID: 0987800f1
```

---

## 🚀 Servicios y Puertos

| Servicio                | Puerto Docker | Puerto Gateway | Endpoint       |
| ----------------------- | ------------- | -------------- | -------------- |
| SpyneIntegrationService | 15070         | 18443          | /api/spyne/\*  |
| Gateway                 | 18443         | -              | API Gateway    |
| Frontend                | 3000          | -              | localhost:3000 |

---

## 🔌 API Endpoints Disponibles

### Via Gateway (http://localhost:18443)

| Método | Endpoint                                    | Descripción                    |
| ------ | ------------------------------------------- | ------------------------------ |
| POST   | `/api/spyne/vehicle-images/transform`       | Transformar una imagen         |
| POST   | `/api/spyne/vehicle-images/transform/batch` | Transformar múltiples imágenes |
| GET    | `/api/spyne/vehicle-images/status/{jobId}`  | Verificar estado del job       |
| GET    | `/api/spyne/vehicle-images/backgrounds`     | Listar fondos disponibles      |
| GET    | `/api/spyne/vehicle-images/health`          | Health check                   |

### Via Servicio Directo (http://localhost:15070)

Mismos endpoints sin el prefijo `/api/spyne`.

---

## 📝 Ejemplos de Uso

### 1. Transformar una imagen

```bash
curl -X POST http://localhost:18443/api/spyne/vehicle-images/transform \
  -H "Content-Type: application/json" \
  -d '{
    "imageUrl": "https://example.com/car.jpg",
    "backgroundId": "75282",
    "maskLicensePlate": true
  }'
```

**Respuesta:**

```json
{
  "jobId": "16c449ce-2477-49aa-a0af-dd5cfa1e8649",
  "status": "processing",
  "message": "Image transformation started. Use the jobId to check status.",
  "estimatedSeconds": 60,
  "checkStatusUrl": "/api/vehicle-images/status/16c449ce-2477-49aa-a0af-dd5cfa1e8649"
}
```

### 2. Verificar estado

```bash
curl http://localhost:18443/api/spyne/vehicle-images/status/{jobId}
```

**Respuesta (procesando):**

```json
{
  "jobId": "16c449ce-2477-49aa-a0af-dd5cfa1e8649",
  "status": "processing",
  "images": null,
  "spin": null,
  "video": null
}
```

**Respuesta (completado):**

```json
{
  "jobId": "16c449ce-2477-49aa-a0af-dd5cfa1e8649",
  "status": "completed",
  "images": [
    {
      "imageId": "abc123",
      "originalUrl": "https://...",
      "processedUrl": "https://cdn.spyne.ai/...",
      "status": "done",
      "category": "exterior",
      "viewAngle": "front"
    }
  ],
  "spin": null,
  "video": null
}
```

### 3. Listar fondos disponibles

```bash
curl http://localhost:18443/api/spyne/vehicle-images/backgrounds
```

---

## 🎨 Background IDs Disponibles

| ID    | Nombre            | Categoría  |
| ----- | ----------------- | ---------- |
| 923   | Studio White      | studio     |
| 924   | Showroom Floor    | showroom   |
| 925   | Outdoor Street    | outdoor    |
| 926   | Luxury Garage     | garage     |
| 927   | Modern Dealership | dealership |
| 928   | Urban Night       | outdoor    |
| 929   | Clean Background  | studio     |
| 75282 | Default Studio    | studio     |

---

## 🔧 Archivos Modificados/Creados

### Backend

1. **SpyneIntegrationService.Domain/Interfaces/ISpyneApiClient.cs**
   - Tipos actualizados para Spyne Unified API v2
   - `SpyneMediaResult` con estructura correcta

2. **SpyneIntegrationService.Infrastructure/Services/SpyneApiClient.cs**
   - Auth cambiado de `x-api-key` a `Authorization: Bearer`
   - Endpoint corregido: `/pv1/merchandise` (no `/get-media`)
   - Payload correcto para Unified Merchandise API

3. **SpyneIntegrationService.Api/Controllers/VehicleImageController.cs**
   - Nuevo controlador REST para transformación de imágenes
   - Endpoints: transform, batch, status, backgrounds

4. **SpyneIntegrationService.Api/Controllers/WebhooksController.cs**
   - Webhook para recibir callbacks de Spyne

5. **Dockerfile.dev**
   - Dockerfile para desarrollo local

### Gateway

6. **Gateway.Api/ocelot.dev.json**
   - Rutas agregadas:
     - `/api/spyne/vehicle-images/{everything}`
     - `/api/spyne/health`
     - `/api/webhooks/spyne`

### Docker

7. **compose.yaml**
   - Servicio `spyneintegrationservice` agregado en puerto 15070

### Frontend

8. **frontend/web/src/services/spyneService.ts**
   - Servicio TypeScript completo
   - Funciones: transformImage, getBackgrounds, pollJobStatus
   - React hook: useSpyneTransform

9. **frontend/web/src/components/dealer/SpyneImageTransform.tsx**
   - Componente React para transformación de imágenes
   - UI con selección de fondos y progress bar

---

## 📊 Flujo de Procesamiento

```
┌──────────────────────────────────────────────────────────────────┐
│                     FLUJO DE TRANSFORMACIÓN                       │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  1. Frontend envía POST /api/spyne/vehicle-images/transform      │
│     └─> Gateway (18443) ─> SpyneIntegrationService (15070)       │
│                                                                  │
│  2. SpyneApiClient llama a Spyne Unified API                     │
│     POST https://api.spyne.ai/api/pv1/merchandise/process        │
│     Headers: Authorization: Bearer {API_KEY}                     │
│                                                                  │
│  3. Spyne devuelve dealerVinID (usado como jobId)                │
│     └─> { "data": { "dealerVinID": "abc-123" } }                │
│                                                                  │
│  4. Frontend hace polling con GET /status/{jobId}                │
│     └─> Cada 2-5 segundos hasta que status = "completed"         │
│                                                                  │
│  5. Cuando Spyne termina, outputData contiene las URLs           │
│     └─> processedUrl = imagen con nuevo fondo                    │
│                                                                  │
│  [OPCIONAL] Spyne también envía webhook a /api/webhooks/spyne    │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## 🔄 Webhook (Opcional)

Para recibir notificaciones cuando Spyne termina de procesar:

1. Configura el webhook URL en Spyne Console:
   - URL: `https://api.okla.com.do/api/webhooks/spyne`
   - Events: `media.processed`

2. El controller `WebhooksController` recibirá el payload v2:

```json
{
  "event": "media.processed",
  "data": {
    "dealerVinId": "...",
    "status": "success",
    "outputData": { ... }
  }
}
```

---

## ⚙️ Variables de Entorno

```yaml
# En compose.yaml
environment:
  SpyneApi__BaseUrl: "https://api.spyne.ai/api"
  SpyneApi__ApiKey: "${SPYNE_API_KEY:-...}"
  SpyneApi__TimeoutSeconds: "120"
  SpyneApi__RetryCount: "3"
```

---

## 🧪 Testing

### Health Check

```bash
curl http://localhost:15070/health
# Respuesta: Healthy
```

### Backgrounds

```bash
curl http://localhost:15070/api/vehicle-images/backgrounds | jq '.[0:3]'
```

### Transform (con imagen pública)

```bash
curl -X POST http://localhost:15070/api/vehicle-images/transform \
  -H "Content-Type: application/json" \
  -d '{
    "imageUrl": "https://upload.wikimedia.org/wikipedia/commons/thumb/1/1b/2019_Toyota_Camry_XSE_in_Silver%2C_front_3.4%2C_03-16-2024.jpg/640px-2019_Toyota_Camry_XSE_in_Silver%2C_front_3.4%2C_03-16-2024.jpg"
  }'
```

---

## 📱 Uso en Frontend

```tsx
import { useSpyneTransform } from "@/services/spyneService";

function VehiclePhotoEditor() {
  const { transform, isLoading, processedImages, error } = useSpyneTransform();

  const handleTransform = async (imageUrl: string) => {
    await transform({
      imageUrl,
      backgroundId: "923", // Studio White
      maskLicensePlate: true,
    });
  };

  return (
    <div>
      {isLoading && <p>Procesando imagen...</p>}
      {processedImages.map((img) => (
        <img key={img.imageId} src={img.processedUrl} alt="Processed" />
      ))}
    </div>
  );
}
```

---

## 🐛 Troubleshooting

### Error: "Invalid Auth key"

- Verificar que se usa `Authorization: Bearer` (no `x-api-key`)
- Verificar que el API key no ha expirado

### Status siempre "processing"

- Spyne puede tomar de 30 segundos a varios minutos
- Verificar logs de Spyne Console para ver si hay errores
- Considerar usar webhooks para notificación

### 404 Not Found en get-media

- El endpoint correcto es `/pv1/merchandise` (no `/get-media`)
- El jobId debe ser el `dealerVinID` retornado por Spyne

### Contenedor no inicia

```bash
docker logs spyneintegrationservice
docker compose up -d spyneintegrationservice --build
```

---

**✅ Configuración Completa - Listo para Usar**
