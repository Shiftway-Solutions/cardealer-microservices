# FASE 4 — Deployment: Dialogflow → LLM (COMPLETADO)

## 🎯 Objetivo

**ELIMINAR Google Dialogflow ES** y reemplazarlo con un modelo **Llama 3 8B fine-tuned** servido via **llama.cpp**.

## 📋 Resumen de Cambios

### ✅ ELIMINADO — Google Dialogflow ES

- ❌ `Google.Cloud.Dialogflow.V2` NuGet package
- ❌ `Google.Apis.Auth` NuGet package
- ❌ `DialogflowService.cs` — Servicio que llamaba a Dialogflow API
- ❌ `IDialogflowService` — Interface con 7 métodos Dialogflow
- ❌ `DialogflowDetectionResult` — Modelo de respuesta Dialogflow
- ❌ `DialogflowAgentInfo` — Info del agente Dialogflow
- ❌ `DialogflowHealthStatus` — Health del servicio Dialogflow
- ❌ `DialogflowSettings` — Configuración (ProjectId, Credentials, etc.)
- ❌ Sección `"Dialogflow"` en `appsettings.json`

### ✅ CREADO — LLM Inference Server

- ✅ `LlmServer/server.py` — Servidor FastAPI compatible con OpenAI API
- ✅ `LlmServer/Dockerfile` — Container para llama.cpp
- ✅ `LlmServer/requirements.txt` — Dependencias Python

### ✅ CREADO — Servicios C# Nuevos

- ✅ `ILlmService` — Interface con 5 métodos LLM
- ✅ `LlmService.cs` — Implementación con HTTP client + Polly resilience
- ✅ `LlmSettings` — Configuración (ServerUrl, ModelId, Temperature, etc.)
- ✅ `LlmDetectionResult` — Modelo de respuesta con campos adicionales
- ✅ `LlmModelInfo` — Información del modelo GGUF
- ✅ `LlmHealthStatus` — Health con métricas detalladas
- ✅ `LlmLeadSignals` — Señales de lead del modelo
- ✅ `LlmChatMessage` — Mensajes para contexto

### ✅ MODIFICADO — Servicios Existentes

| Archivo                         | Cambio                                                                                 |
| ------------------------------- | -------------------------------------------------------------------------------------- |
| `IServices.cs`                  | `IDialogflowService` → `ILlmService` (5 métodos)                                       |
| `ServiceModels.cs`              | Todas las clases `Dialogflow*` → `Llm*`                                                |
| `DependencyInjection.cs`        | DI: `ILlmService` → `LlmService` + `LlmServer` HttpClient                              |
| `SessionCommandHandlers.cs`     | Pipeline: `_dialogflowService.DetectIntentAsync` → `_llmService.GenerateResponseAsync` |
| `AutoLearningService.cs`        | `IDialogflowService` → `ILlmService`, Dialogflow API calls → training data storage     |
| `HealthMonitoringService.cs`    | `CheckDialogflowHealthAsync` → `CheckLlmHealthAsync`                                   |
| `MaintenanceCommandHandlers.cs` | `IDialogflowService` → `ILlmService`                                                   |
| `MaintenanceCommands.cs`        | `DialogflowProjectId` → `LlmServerUrl`, `BypassDialogflow` → `BypassLlm`               |
| `IRepositories.cs`              | + `GetRecentBySessionTokenAsync`, + `GetLlmCallsCountAsync`                            |
| `Repositories.cs`               | Implementación de nuevos métodos del repositorio                                       |
| `appsettings.json`              | Sección `"Dialogflow"` → `"LlmService"`                                                |
| `Infrastructure.csproj`         | Removidos NuGet packages de Google Dialogflow                                          |

### ✅ CREADO — Infraestructura

- ✅ `docker-compose.llm.yml` — Docker Compose para desarrollo local
- ✅ `k8s/llm-server.yaml` — Kubernetes manifests (Deployment, Service, PVC, ConfigMap, Job)

---

## 🏗️ Arquitectura

```
┌─────────────────┐     HTTP POST     ┌─────────────────┐
│   ChatbotService│ ────────────────▶ │   LLM Server     │
│   (.NET 8)      │  /v1/chat/        │   (llama.cpp)    │
│                 │  completions      │                   │
│   LlmService.cs │ ◀──────────────── │   server.py       │
│   (HttpClient   │     JSON          │   FastAPI          │
│   + Polly)      │                   │                   │
└─────────────────┘                   └─────────────────┘
                                            │
                                      ┌─────┴──────┐
                                      │ GGUF Model  │
                                      │ Q4_K_M      │
                                      │ ~4.5 GB     │
                                      └─────────────┘
```

### Pipeline de Mensajes (ANTES vs DESPUÉS)

**ANTES (Dialogflow):**

```
Usuario → QuickResponse check → Dialogflow ES API → Parse intent → Save → Response
```

**DESPUÉS (LLM):**

```
Usuario → QuickResponse check → LLM Server (llama.cpp) → Parse JSON → Save → Response
```

### Formato de Respuesta del LLM

El modelo está fine-tuned para responder en JSON:

```json
{
  "response": "¡Claro! Tenemos un Toyota Corolla 2023...",
  "intent": "vehicle_inquiry",
  "confidence": 0.95,
  "parameters": {
    "make": "Toyota",
    "model": "Corolla",
    "year": "2023"
  },
  "leadSignals": {
    "purchaseIntent": 0.8,
    "urgency": 0.6,
    "preferredContact": "whatsapp",
    "shouldTransferToAgent": false
  }
}
```

---

## 🚀 Deployment

### Desarrollo Local

```bash
# 1. Descargar el modelo GGUF (~4.5 GB)
mkdir -p backend/ChatbotService/LlmServer/models
# Copiar okla-llama3-8b-q4_k_m.gguf al directorio models/

# 2. Levantar con Docker Compose
cd backend/ChatbotService
docker compose -f docker-compose.llm.yml up -d

# 3. Verificar salud del servidor LLM
curl http://localhost:8000/health
# → {"status": "healthy", "model_loaded": true, ...}

# 4. Probar inferencia
curl -X POST http://localhost:8000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "messages": [
      {"role": "system", "content": "Eres OKLA Assistant."},
      {"role": "user", "content": "¿Tienen Toyota Corolla disponible?"}
    ]
  }'
```

### Kubernetes (DOKS)

```bash
# 1. Descargar modelo al PersistentVolume
kubectl apply -f k8s/llm-server.yaml
kubectl wait --for=condition=complete job/download-llm-model -n okla --timeout=600s

# 2. Verificar deployment
kubectl get pods -n okla -l app=llm-server
kubectl logs -f deployment/llm-server -n okla

# 3. Health check interno
kubectl exec -it deployment/chatbotservice -n okla -- \
  curl http://llm-server:8000/health
```

---

## 📊 Comparación: Dialogflow vs LLM

| Aspecto             | Dialogflow ES            | LLM (Llama 3 8B)                 |
| ------------------- | ------------------------ | -------------------------------- |
| **Costo**           | $0.002/request + escala  | Fijo (~$50/mes servidor)         |
| **Latencia**        | ~200-500ms               | ~500-2000ms (CPU)                |
| **Idioma**          | Multi (pero genérico)    | Dominican Spanish (fine-tuned)   |
| **Contexto**        | Limitado a contexts      | Full conversation history        |
| **Intents**         | Predefinidos manualmente | Detectados automáticamente       |
| **Personalización** | Limitada                 | Total (re-entrenamiento)         |
| **Vendor lock-in**  | Google Cloud             | Self-hosted                      |
| **Compliance**      | Datos en Google          | Datos en nuestros servidores     |
| **Lead scoring**    | No nativo                | Integrado en modelo              |
| **Legal RD**        | No conoce leyes RD       | Entrenado con Ley 358-05, 172-13 |

---

## ⚠️ Notas Importantes

1. **Backward Compatibility DB:** Los campos `DialogflowIntentName` en la entidad `ChatMessage` mantienen el nombre de columna en la base de datos para no requerir migración destructiva. El campo almacena ahora el intent detectado por el LLM.

2. **Auto-Learning:** Las funciones `CreateIntentAsync`, `AddTrainingPhrasesAsync`, y `TrainAgentAsync` que llamaban a Dialogflow API ahora almacenan las sugerencias para futuros ciclos de re-entrenamiento del modelo LLM.

3. **Modelo GGUF:** El archivo `okla-llama3-8b-q4_k_m.gguf` (~4.5 GB) debe descargarse desde HuggingFace Hub y montarse como volumen. No se incluye en la imagen Docker.

4. **GPU Support:** Por defecto `N_GPU_LAYERS=0` (CPU only). Para GPU, configurar a `-1` (all layers) y usar una imagen Docker con CUDA support.

---

_FASE 4 completada — Google Dialogflow ES ELIMINADO del sistema_
