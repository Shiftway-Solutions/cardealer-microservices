# 🤖 Chatbot LLM — Configuración con Metal GPU (macOS)

**Fecha:** Febrero 17, 2026  
**Autor:** Equipo de Desarrollo OKLA  
**Estado:** ✅ Funcionando en desarrollo local

---

## 📋 Resumen Ejecutivo

El chatbot de OKLA utiliza un modelo **LLaMA 3 8B fine-tuned** (formato GGUF, cuantización Q4_K_M) para responder consultas de usuarios sobre vehículos. Inicialmente desplegado en Docker con inferencia por CPU, el modelo experimentaba **crashes SIGSEGV** en la arquitectura aarch64 (Apple Silicon) y tiempos de respuesta inaceptables (~4.5 minutos por mensaje).

La solución definitiva para desarrollo local fue migrar la inferencia a **ejecución nativa en macOS con aceleración Metal GPU**, reduciendo los tiempos de respuesta de **4.5 minutos a ~5 segundos** (mejora de ~54x).

---

## 🏗️ Arquitectura del Chatbot

```
┌─────────────┐     ┌──────────────┐     ┌──────────────────┐     ┌──────────────────┐
│  Frontend   │     │   Gateway    │     │  ChatbotService  │     │   LLM Server     │
│  Next.js    │────▶│   Ocelot     │────▶│   .NET 8         │────▶│   FastAPI +      │
│  ChatWidget │     │  :18443      │     │   Docker :5060   │     │   llama-cpp      │
│             │     │              │     │                  │     │   Metal GPU      │
└─────────────┘     └──────────────┘     └──────────────────┘     │   Nativo :8000   │
                                                                  └──────────────────┘
```

### Componentes

| Componente         | Tecnología                 | Ubicación        | Puerto       |
| ------------------ | -------------------------- | ---------------- | ------------ |
| **Frontend**       | Next.js 14 + ChatWidget    | Docker           | 3000         |
| **Gateway**        | Ocelot (.NET 8)            | Docker           | 18443        |
| **ChatbotService** | .NET 8 + MediatR + EF Core | Docker           | 5060 (→8080) |
| **LLM Server**     | FastAPI + llama-cpp-python | **Nativo macOS** | 8000         |
| **PostgreSQL**     | PostgreSQL 16 Alpine       | Docker           | 5434 (→5432) |
| **Redis**          | Redis 7 Alpine             | Docker           | 6380 (→6379) |

---

## 🔴 Problema Original: SIGSEGV en Docker aarch64

### Síntomas

1. El LLM server crasheaba con `SIGSEGV` (segmentation fault) durante la inferencia
2. Error específico: `ggml_compute_forward_set_rows` en tensores aarch64
3. El servidor se reiniciaba automáticamente (`restart: unless-stopped`), causando múltiples intentos
4. Polly (circuit breaker + retries) amplificaba el problema generando 3-6 requests concurrentes al LLM
5. El frontend auto-reintentaba, duplicando mensajes en la UI

### Causa raíz

- **Bug en llama.cpp compilado para aarch64 Linux Docker** — la operación `set_rows` causa SIGSEGV en kernels GGML optimizados para ARM64 bajo emulación Docker
- El modelo GGUF tiene 291 tensores pero `llama-cpp-python==0.2.83` esperaba 292, causando incompatibilidad de formato
- La emulación x86_64 via Rosetta funcionaba pero con rendimiento inaceptable (~4.5 min para 30 tokens)

### Intentos fallidos

| Intento                           | Resultado                                                                     |
| --------------------------------- | ----------------------------------------------------------------------------- |
| Pinear `llama-cpp-python==0.2.83` | `wrong number of tensors; expected 292, got 291` — incompatible con el modelo |
| Reducir `N_THREADS=1`             | SIGSEGV persiste — no es un race condition                                    |
| Reducir `N_CTX=512`, `N_BATCH=32` | SIGSEGV persiste — no es un problema de memoria                               |
| `platform: linux/amd64` (Rosetta) | Funciona pero 4.5 min/30 tokens — inaceptable                                 |

---

## ✅ Solución: LLM Nativo con Metal GPU

### Rendimiento comparativo

| Métrica     | Docker CPU (aarch64)  | Docker CPU (x86/Rosetta) | **Metal GPU (nativo)**  |
| ----------- | --------------------- | ------------------------ | ----------------------- |
| 30 tokens   | ❌ SIGSEGV crash      | ~4.5 minutos             | **~1.5 segundos**       |
| 100 tokens  | ❌ SIGSEGV crash      | ~15 minutos (estimado)   | **~5 segundos**         |
| 200 tokens  | ❌ SIGSEGV crash      | ~30 minutos (estimado)   | **~10 segundos**        |
| Estabilidad | ❌ Crashes constantes | ✅ Estable pero lento    | **✅ Estable y rápido** |
| GPU Layers  | 0 (CPU only)          | 0 (CPU only)             | **99 (full GPU)**       |

---

## 🚀 Guía de Inicio Rápido

### Prerrequisitos

- macOS con Apple Silicon (M1/M2/M3/M4)
- Docker Desktop para macOS
- Python 3.11+ instalado via Homebrew
- Modelo GGUF descargado en `backend/ChatbotService/LlmServer/models/`

### Paso 1: Iniciar el LLM Server (Nativo)

```bash
cd backend/ChatbotService/LlmServer
./start-background.sh
```

Esto:

1. Mata cualquier proceso en el puerto 8000
2. Carga el modelo con Metal GPU (99 capas en GPU)
3. Espera hasta que el health check responda
4. Muestra el estado del servidor

**Verificar manualmente:**

```bash
curl http://localhost:8000/health | python3 -m json.tool
```

### Paso 2: Iniciar ChatbotService + Dependencias (Docker)

```bash
cd backend/ChatbotService
docker compose up -d chatbot-db chatbot-redis chatbotservice
```

> ⚠️ **NO iniciar `llm-server`** desde docker-compose. El LLM corre nativo.

**Verificar:**

```bash
docker compose ps
# Los 3 contenedores deben estar "healthy"
```

### Paso 3: Asegurar que el Gateway esté corriendo

```bash
cd /path/to/cardealer-microservices
docker compose up -d gateway
```

### Paso 4: Probar el chatbot

**Via curl:**

```bash
# 1. Crear sesión
curl -s -X POST http://localhost:18443/api/chat/start \
  -H "Content-Type: application/json" \
  -d '{"sessionType":"WebChat","metadata":{"source":"test"}}' | python3 -m json.tool

# 2. Enviar mensaje (usar sessionToken de la respuesta anterior)
curl -s -X POST http://localhost:18443/api/chat/message \
  -H "Content-Type: application/json" \
  -d '{"sessionToken":"<TOKEN>","message":"Que vehiculos tienes?"}' | python3 -m json.tool
```

**Via frontend:**

1. Abrir http://localhost:3000
2. Click en el ícono de chat (burbuja 💬) en la esquina inferior derecha
3. Enviar un mensaje

**Via test HTML:**

1. Abrir `backend/ChatbotService/chat-test.html` en un navegador
2. Seleccionar un dealer
3. Enviar mensajes directamente al ChatbotService (puerto 5060)

### Paso 5: Detener el LLM Server

```bash
kill $(cat /tmp/llm-server.pid)
```

---

## 📁 Archivos Clave

### Scripts de inicio

| Archivo                         | Descripción                                                 |
| ------------------------------- | ----------------------------------------------------------- |
| `LlmServer/start-background.sh` | Inicia LLM en background con Metal GPU, espera health check |
| `LlmServer/start-native.sh`     | Inicia LLM en foreground (para debugging)                   |

### Configuración Docker

| Archivo                      | Cambios realizados                                                                                  |
| ---------------------------- | --------------------------------------------------------------------------------------------------- |
| `docker-compose.yml`         | `LlmService__ServerUrl` apunta a `host.docker.internal:8000`; eliminada dependencia de `llm-server` |
| `LlmServer/Dockerfile`       | Sin cambios (usado solo para Kubernetes/producción)                                                 |
| `LlmServer/requirements.txt` | `llama-cpp-python>=0.3.0` (actualizado de `==0.2.83`)                                               |

### Código del LLM Server

| Archivo                                       | Descripción                                                                          |
| --------------------------------------------- | ------------------------------------------------------------------------------------ |
| `LlmServer/server.py`                         | FastAPI server, carga modelo GGUF, expone `/v1/chat/completions` (OpenAI-compatible) |
| `LlmServer/models/okla-llama3-8b-q4_k_m.gguf` | Modelo fine-tuned (4.6 GB, Q4_K_M)                                                   |
| `LlmServer/.venv/`                            | Virtual environment con llama-cpp-python compilado con Metal                         |

### Código del ChatbotService

| Archivo                                                | Cambios realizados                                                                                          |
| ------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------- |
| `ChatbotService.Infrastructure/Services/LlmService.cs` | Timeout 60→600s (Docker) / 60s (Metal), retries eliminados (`Policy.NoOpAsync()`), circuit breaker relajado |
| `ChatbotService.Api/Program.cs`                        | `JsonStringEnumConverter` para serializar enums como strings                                                |

### Gateway

| Archivo                                              | Cambios realizados                                                                              |
| ---------------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| `Gateway.Api/ocelot.dev.json`                        | Rutas `/api/chat/*` agregadas, apuntan a `host.docker.internal:5060`, rutas públicas (sin auth) |
| `Gateway.Api/Middleware/CsrfValidationMiddleware.cs` | Endpoints chat exemptions de CSRF                                                               |

### Frontend

| Archivo                                     | Cambios realizados                          |
| ------------------------------------------- | ------------------------------------------- |
| `frontend/web-next/src/services/chatbot.ts` | Timeout de axios reducido a 60s             |
| `frontend/web-next/src/hooks/useChatbot.ts` | Eliminado auto-retry que duplicaba mensajes |

---

## 🔧 Configuración Detallada

### Entorno Virtual del LLM (Python)

```bash
cd backend/ChatbotService/LlmServer

# Crear venv (solo primera vez)
python3 -m venv .venv

# Instalar con soporte Metal
source .venv/bin/activate
CMAKE_ARGS="-DGGML_METAL=on" pip install llama-cpp-python fastapi uvicorn pydantic prometheus-client
```

La flag `CMAKE_ARGS="-DGGML_METAL=on"` compila llama.cpp con soporte para Apple Metal, habilitando aceleración GPU en chips Apple Silicon.

### Variables de Entorno del LLM Server

| Variable       | Valor Dev (Metal)                     | Valor Docker (CPU)                   | Descripción                           |
| -------------- | ------------------------------------- | ------------------------------------ | ------------------------------------- |
| `MODEL_PATH`   | `./models/okla-llama3-8b-q4_k_m.gguf` | `/models/okla-llama3-8b-q4_k_m.gguf` | Ruta al modelo GGUF                   |
| `HOST`         | `0.0.0.0`                             | `0.0.0.0`                            | Bind address                          |
| `PORT`         | `8000`                                | `8000`                               | Puerto HTTP                           |
| `N_CTX`        | `2048`                                | `512`                                | Ventana de contexto (tokens)          |
| `N_GPU_LAYERS` | `99`                                  | `0`                                  | Capas offloaded a GPU (99=todas)      |
| `N_THREADS`    | `4`                                   | `4`                                  | Threads CPU (para operaciones no-GPU) |
| `N_BATCH`      | `512`                                 | `32`                                 | Batch size para prompt processing     |
| `MAX_TOKENS`   | `200`                                 | `100`                                | Máximo tokens por respuesta           |

### Conexión ChatbotService → LLM

En `docker-compose.yml`:

```yaml
chatbotservice:
  environment:
    # Desarrollo local: LLM nativo en macOS
    LlmService__ServerUrl: "http://host.docker.internal:8000"
    LlmService__MaxTokens: "200"
    LlmService__TimeoutSeconds: "60"
```

> `host.docker.internal` resuelve a la IP del host macOS desde dentro de Docker.

### Rutas del Gateway para Chat

En `ocelot.dev.json`, las rutas `/api/chat/*` son **públicas** (sin autenticación JWT):

```json
{
  "UpstreamPathTemplate": "/api/chat/message",
  "DownstreamPathTemplate": "/api/chat/message",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "host.docker.internal", "Port": 5060 }],
  "UpstreamHttpMethod": ["POST"],
  "AuthenticationOptions": { "AuthenticationProviderKey": "" }
}
```

Endpoints chat en Gateway:

- `POST /api/chat/start` — Crear sesión
- `POST /api/chat/message` — Enviar mensaje
- `POST /api/chat/end` — Terminar sesión
- `POST /api/chat/transfer` — Transferir a agente humano
- `GET /api/chat/session/{token}` — Info de sesión
- `GET /api/chat/health` — Health check del chatbot

---

## 🔄 Fixes Aplicados Durante la Implementación

### 1. Gateway: Rutas del Chatbot (404 → 200)

**Problema:** El frontend llamaba a `/api/chat/start` pero el Gateway solo tenía rutas para `/api/chatbot/*` (admin).

**Fix:** Agregadas 6 rutas específicas para `/api/chat/*` en `ocelot.dev.json` apuntando a `host.docker.internal:5060`.

### 2. Gateway: CSRF Exemption (403 → 200)

**Problema:** El middleware CSRF bloqueaba POSTs sin token `X-CSRF-Token`.

**Fix:** Agregados `/api/chat/start`, `/api/chat/message`, `/api/chat/end`, `/api/chat/transfer` a `ExemptPaths` en `CsrfValidationMiddleware.cs`.

### 3. ChatbotService: JSON Enum Serialization (400 → 200)

**Problema:** Frontend enviaba `"sessionType": "WebChat"` (string) pero C# esperaba el valor numérico del enum.

**Fix:** Agregado `JsonStringEnumConverter` a `Program.cs`:

```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
```

### 4. LlmService: Eliminación de Retries (Duplicados → Single request)

**Problema:** Polly reintentaba 3 veces con backoff exponencial, generando múltiples requests al LLM crasheante.

**Fix:** Cambiado de retry policy a `Policy.NoOpAsync()`:

```csharp
// Antes: 3 retries con backoff exponencial
private readonly IAsyncPolicy _retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

// Después: sin retries
private readonly IAsyncPolicy _retryPolicy = Policy.NoOpAsync();
```

### 5. Frontend: Eliminación de Auto-Retry (Mensajes duplicados)

**Problema:** `useChatbot.ts` auto-reintentaba con `setTimeout` hasta `maxRetries` veces, duplicando mensajes en la UI.

**Fix:** Eliminada lógica de auto-retry en `handleSendMessage`.

### 6. llama-cpp-python: Actualización de Versión (Tensor mismatch)

**Problema:** Versión `0.2.83` reportaba `wrong number of tensors; expected 292, got 291` al cargar el modelo.

**Fix:** Actualizado a `>=0.3.0` (instala `0.3.16`), que soporta el formato GGUF del modelo.

---

## 🐛 Troubleshooting

### LLM Server no inicia

```bash
# Verificar que el puerto 8000 está libre
lsof -ti:8000

# Matar proceso existente
kill -9 $(lsof -ti:8000)

# Verificar que el modelo existe
ls -lh backend/ChatbotService/LlmServer/models/okla-llama3-8b-q4_k_m.gguf
# Esperado: ~4.6 GB

# Verificar logs
cat /tmp/llm-server.log
```

### ChatbotService no conecta al LLM

```bash
# Verificar que host.docker.internal resuelve desde el contenedor
docker exec chatbotservice curl -s http://host.docker.internal:8000/health

# Verificar logs del chatbotservice
docker logs chatbotservice 2>&1 | grep -i "llm\|error" | tail -20
```

### Circuit Breaker abierto

Si el LLM crasheó previamente, el circuit breaker de Polly puede estar abierto y rechazar nuevos requests. Solución:

```bash
docker compose restart chatbotservice
```

### "Session not found" al enviar mensaje

El endpoint `/api/chat/message` espera `sessionToken` en el body JSON (NO `sessionId` ni header):

```json
{
  "sessionToken": "abc123...",
  "message": "Hola"
}
```

### Modelo descargado / ubicación

El modelo GGUF debe estar en:

```
backend/ChatbotService/LlmServer/models/okla-llama3-8b-q4_k_m.gguf
```

Peso: ~4.6 GB, cuantización Q4_K_M, basado en LLaMA 3 8B fine-tuned para OKLA.

---

## 🚢 Consideraciones para Producción (Kubernetes)

En producción (DOKS), el LLM server corre como contenedor Docker con CPU (no Metal):

- La imagen usa `platform: linux/amd64` en nodos x86_64 del cluster
- GPU offloading no está disponible (nodos sin GPU)
- Se recomienda evaluar nodos con GPU (NVIDIA) o APIs externas (OpenAI, Anthropic) para producción
- El contenedor Docker del LLM sigue siendo válido para deployment en servidores x86_64 con suficiente RAM (10GB+)
- Para producción, considerar usar un modelo más pequeño (3B) o un servicio de inferencia gestionado

### Alternativa: API Externa en Producción

Si el rendimiento CPU no es aceptable en producción, se puede configurar `LlmService` para usar una API externa:

```yaml
# k8s/configmaps.yaml
LlmService__ServerUrl: "https://api.openai.com/v1"
LlmService__ModelId: "gpt-4o-mini"
LlmService__ApiKey: "${OPENAI_API_KEY}" # Desde Kubernetes Secret
```

---

## 📊 Endpoints del LLM Server

| Endpoint               | Método | Descripción                         |
| ---------------------- | ------ | ----------------------------------- |
| `/v1/chat/completions` | POST   | Chat completion (OpenAI-compatible) |
| `/health`              | GET    | Health check con métricas           |
| `/info`                | GET    | Información del modelo cargado      |
| `/metrics`             | GET    | Métricas Prometheus                 |

### Ejemplo de request

```bash
curl -X POST http://localhost:8000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "messages": [
      {"role": "system", "content": "Eres Ana, asistente de ventas de Auto Dominicana Premium."},
      {"role": "user", "content": "Que vehiculos tienes disponibles?"}
    ],
    "max_tokens": 200,
    "temperature": 0.7
  }'
```

### Ejemplo de respuesta

```json
{
  "id": "chatcmpl-603920d198c1",
  "object": "chat.completion",
  "created": 1771318908,
  "model": "okla-llama3-8b",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "¡Excelente! Tenemos varias opciones:\n\n1. **Toyota Land Cruiser Prado 2023 VX** - RD$5,800,000\n2. **Ford Bronco Sport 2024 Big Bend** - RD$2,700,000\n3. **Kia Carnival 2024 EX** - RD$3,300,000\n\n¿Te interesa alguno en particular?"
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 36,
    "completion_tokens": 85,
    "total_tokens": 121
  }
}
```

---

## 📝 Especificaciones del Modelo

| Propiedad                     | Valor               |
| ----------------------------- | ------------------- | ------------- | ----------- |
| **Nombre**                    | Okla Llama3 Merged  |
| **Arquitectura**              | LLaMA               |
| **Parámetros**                | 8.03 B              |
| **Formato**                   | GGUF V3             |
| **Cuantización**              | Q4_K_M (4.89 BPW)   |
| **Tamaño en disco**           | 4.58 GiB            |
| **Vocabulario**               | BPE, 128,256 tokens |
| **Contexto de entrenamiento** | 131,072 tokens      |
| **BOS Token**                 | `<                  | begin_of_text | >` (128000) |
| **EOS Token**                 | `<                  | eot_id        | >` (128009) |
| **Chat Format**               | llama-3             |

---

_Documento actualizado: Febrero 17, 2026_
