# 🤖 Plan de Implementación — Chatbot OKLA con Llama 3

**Fecha:** Febrero 15, 2026
**Objetivo:** Implementar un chatbot IA de ventas vehiculares usando Llama 3 8B fine-tuned, desplegado en DOKS, integrado en la plataforma OKLA.

---

## 📊 Estado Actual (Auditoría)

| Componente                          |   Estado   | Detalle                                                                  |
| ----------------------------------- | :--------: | ------------------------------------------------------------------------ |
| **FASE 1** — 10 Prompts modulares   |  ✅ 100%   | 10 archivos .md con prompts, ejemplos, código .NET                       |
| **FASE 2** — Dataset sintético      |  ✅ 100%   | 2,989 conversaciones JSONL generadas (train/eval/test)                   |
| **FASE 3** — Notebook fine-tuning   |  ✅ 100%   | 32 celdas listas para Colab (QLoRA, Llama 3 8B)                          |
| **FASE 4** — Backend ChatbotService |  ✅ ~90%   | Clean Architecture completa, `LlmService.cs`, `server.py`, K8s manifests |
| **FASE 4** — Gateway routes         |   ❌ 0%    | No hay rutas chatbot en `ocelot.*.json`                                  |
| **FASE 4** — Docker Compose         |   ❌ 0%    | No hay entry en `docker-compose.yml` principal                           |
| **FASE 5** — MLOps scripts          |  ✅ 100%   | evaluation, feedback, drift, monitoring, A/B testing                     |
| **Frontend** — Widget chatbot       |   ❌ 0%    | No existe componente en `web-next`                                       |
| **Frontend** — Servicio API chat    | 🟡 Parcial | Existe en `_DESCARTADOS` (489 líneas, Vite) — necesita port a Next.js    |
| **Modelo entrenado** — GGUF         |   ❌ 0%    | Necesita ejecución en GPU (Colab)                                        |

---

## 🚀 Plan por Pasos (10 Pasos)

### PASO 1: Entrenar el Modelo en Google Colab ⏱️ ~3 horas

**Qué hacer:**

1. Abrir `FASE_3_TRAINING/okla_finetune_llama3.ipynb` en VS Code
2. Conectar a Google Colab (extensión VS Code Colab o directamente en colab.google.com)
3. Subir dataset a Google Drive:
   ```
   Drive/OKLA/dataset/
   ├── okla_train.jsonl  (13.4 MB)
   ├── okla_eval.jsonl   (1.68 MB)
   └── okla_test.jsonl   (1.66 MB)
   ```
4. Ejecutar las 21 celdas de código secuencialmente
5. Verificar métricas de calidad:
   - Eval loss < 1.5 (ideal < 0.8)
   - Perplexity < 5.0 (ideal < 2.5)
   - JSON válido > 85%
   - Intent correcto > 70%
6. Descargar el modelo GGUF Q4_K_M (~4.7 GB) desde Drive

**Output:** `okla-llama3-8b-chatbot.Q4_K_M.gguf` (~4.7 GB)

**Dónde colocarlo:** `backend/ChatbotService/LlmServer/models/`

> ⚠️ **Alternativa sin GPU:** Si no tienes acceso a Colab con GPU T4/A100, puedes:
>
> - Usar RunPod.io (~$0.50/hora con A100)
> - Usar Lambda Labs (~$1.10/hora con A10)
> - Usar Vast.ai (~$0.30/hora con RTX 4090)

---

### PASO 2: Probar el LLM Server Localmente ⏱️ ~30 min

**Qué hacer:**

1. Colocar el modelo GGUF en `backend/ChatbotService/LlmServer/models/`
2. Ejecutar el LLM server localmente:
   ```bash
   cd backend/ChatbotService
   docker compose -f docker-compose.llm.yml up llm-server
   ```
3. Probar el endpoint:
   ```bash
   curl -X POST http://localhost:8000/v1/chat/completions \
     -H "Content-Type: application/json" \
     -d '{
       "messages": [
         {"role": "system", "content": "Eres OKLA Bot, asistente de ventas vehiculares en RD."},
         {"role": "user", "content": "Klk, busco una yipeta pa la familia"}
       ],
       "temperature": 0.7,
       "max_tokens": 512
     }'
   ```
4. Verificar que responde JSON con `intent`, `confidence`, `response`, `leadSignals`
5. Verificar health: `curl http://localhost:8000/health`

**Criterios de éxito:**

- ✅ Server arranca en < 2 minutos
- ✅ Responde en < 5 segundos
- ✅ JSON válido con todos los campos
- ✅ Entiende slang dominicano (yipeta, guagua, pela'o)

---

### PASO 3: Agregar ChatbotService al Docker Compose Principal ⏱️ ~15 min

**Qué hacer:**

1. Agregar al `docker-compose.yml` principal:

   ```yaml
   chatbotservice:
     build:
       context: ./backend/ChatbotService
       dockerfile: Dockerfile
     ports:
       - "5060:8080"
     environment:
       - ASPNETCORE_ENVIRONMENT=Docker
       - ConnectionStrings__DefaultConnection=Host=postgres;Database=chatbot_db;Username=postgres;Password=postgres123
       - LlmService__ServerUrl=http://llm-server:8000
       - RabbitMq__HostName=rabbitmq
       - Redis__ConnectionString=redis:6379
     depends_on:
       postgres:
         condition: service_healthy
       llm-server:
         condition: service_healthy
     networks:
       - cardealer-network

   llm-server:
     build:
       context: ./backend/ChatbotService/LlmServer
       dockerfile: Dockerfile
     ports:
       - "8000:8000"
     volumes:
       - ./backend/ChatbotService/LlmServer/models:/app/models
     environment:
       - MODEL_PATH=/app/models/okla-llama3-8b-chatbot.Q4_K_M.gguf
       - N_CTX=2048
       - N_GPU_LAYERS=0
       - N_THREADS=4
     deploy:
       resources:
         limits:
           memory: 8G
     healthcheck:
       test: ["CMD", "curl", "-f", "http://localhost:8000/health"]
       interval: 30s
       timeout: 10s
       start_period: 120s
       retries: 3
     networks:
       - cardealer-network
   ```

2. Verificar que arranca con: `docker compose up chatbotservice llm-server`

---

### PASO 4: Agregar Rutas al Gateway (Ocelot) ⏱️ ~20 min

**Qué hacer:**

1. Agregar las siguientes rutas a `ocelot.Development.json` y `ocelot.prod.json`:

```json
{
  "Routes": [
    {
      "UpstreamPathTemplate": "/api/chat/sessions",
      "UpstreamHttpMethod": ["POST"],
      "DownstreamPathTemplate": "/api/chat/sessions",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [{ "Host": "chatbotservice", "Port": 8080 }],
      "AuthenticationOptions": { "AuthenticationProviderKey": "Bearer" }
    },
    {
      "UpstreamPathTemplate": "/api/chat/sessions/{sessionId}/messages",
      "UpstreamHttpMethod": ["POST"],
      "DownstreamPathTemplate": "/api/chat/sessions/{sessionId}/messages",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [{ "Host": "chatbotservice", "Port": 8080 }],
      "AuthenticationOptions": { "AuthenticationProviderKey": "Bearer" }
    },
    {
      "UpstreamPathTemplate": "/api/chat/sessions/{sessionId}/end",
      "UpstreamHttpMethod": ["POST"],
      "DownstreamPathTemplate": "/api/chat/sessions/{sessionId}/end",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [{ "Host": "chatbotservice", "Port": 8080 }],
      "AuthenticationOptions": { "AuthenticationProviderKey": "Bearer" }
    },
    {
      "UpstreamPathTemplate": "/api/chat/sessions/{sessionId}/transfer",
      "UpstreamHttpMethod": ["POST"],
      "DownstreamPathTemplate": "/api/chat/sessions/{sessionId}/transfer",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [{ "Host": "chatbotservice", "Port": 8080 }],
      "AuthenticationOptions": { "AuthenticationProviderKey": "Bearer" }
    },
    {
      "UpstreamPathTemplate": "/api/chat/sessions/{sessionId}",
      "UpstreamHttpMethod": ["GET"],
      "DownstreamPathTemplate": "/api/chat/sessions/{sessionId}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [{ "Host": "chatbotservice", "Port": 8080 }],
      "AuthenticationOptions": { "AuthenticationProviderKey": "Bearer" }
    },
    {
      "UpstreamPathTemplate": "/api/chat/sessions/{sessionId}/messages",
      "UpstreamHttpMethod": ["GET"],
      "DownstreamPathTemplate": "/api/chat/sessions/{sessionId}/messages",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [{ "Host": "chatbotservice", "Port": 8080 }],
      "AuthenticationOptions": { "AuthenticationProviderKey": "Bearer" }
    },
    {
      "UpstreamPathTemplate": "/api/chat/configuration/{configId}",
      "UpstreamHttpMethod": ["GET", "PUT"],
      "DownstreamPathTemplate": "/api/chat/configuration/{configId}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [{ "Host": "chatbotservice", "Port": 8080 }]
    },
    {
      "UpstreamPathTemplate": "/api/chat/configuration/dealer/{dealerId}",
      "UpstreamHttpMethod": ["GET"],
      "DownstreamPathTemplate": "/api/chat/configuration/dealer/{dealerId}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [{ "Host": "chatbotservice", "Port": 8080 }]
    }
  ]
}
```

2. Reiniciar el Gateway: `docker compose restart gateway`
3. Verificar conectividad: `curl http://localhost:18443/api/chat/sessions -H "Authorization: Bearer <token>"`

---

### PASO 5: Implementar Widget de Chat en Frontend (Next.js) ⏱️ ~4-6 horas

**Qué crear:**

```
frontend/web-next/src/
├── components/
│   └── chat/
│       ├── ChatWidget.tsx           # Widget flotante (bubble + panel)
│       ├── ChatBubble.tsx           # Botón flotante esquina inferior
│       ├── ChatPanel.tsx            # Panel de conversación
│       ├── ChatMessage.tsx          # Componente de mensaje individual
│       ├── ChatInput.tsx            # Input de texto + enviar
│       ├── ChatHeader.tsx           # Header con nombre + cerrar
│       ├── ChatTypingIndicator.tsx  # "OKLA Bot está escribiendo..."
│       ├── ChatVehicleCard.tsx      # Card de vehículo en chat
│       ├── ChatComparisonTable.tsx  # Tabla comparativa
│       └── index.ts                # Exports
├── services/
│   └── chatbot.ts                  # API client para ChatbotService
├── hooks/
│   └── useChatbot.ts               # Hook principal del chatbot
└── app/
    └── (main)/
        └── layout.tsx              # ← Agregar <ChatWidget /> aquí
```

**Características del widget:**

- 💬 Botón flotante en esquina inferior derecha
- 📱 Panel expandible responsive (mobile: fullscreen, desktop: 400x600px)
- 🔄 Indicador de "escribiendo..."
- 🚗 Cards de vehículos inline con imagen, precio, CTA
- 📊 Tablas comparativas inline
- 🔐 Integración con auth (JWT automático)
- 💾 Persistencia de sesión (localStorage)
- 🔔 Notificación de mensajes nuevos
- 🎨 Temas light/dark (sigue el theme de la app)

---

### PASO 6: Implementar Servicio API del Chat (Frontend) ⏱️ ~2 horas

**API Client (`services/chatbot.ts`):**

```typescript
export const chatbotService = {
  // Sesiones
  startSession: (dealerId: string, channel: string) =>
    api.post("/api/chat/sessions", { dealerId, channel }),

  sendMessage: (sessionId: string, message: string) =>
    api.post(`/api/chat/sessions/${sessionId}/messages`, { content: message }),

  endSession: (sessionId: string) =>
    api.post(`/api/chat/sessions/${sessionId}/end`),

  getSession: (sessionId: string) => api.get(`/api/chat/sessions/${sessionId}`),

  getMessages: (sessionId: string) =>
    api.get(`/api/chat/sessions/${sessionId}/messages`),

  transferToAgent: (sessionId: string, reason: string) =>
    api.post(`/api/chat/sessions/${sessionId}/transfer`, { reason }),
};
```

**Custom Hook (`hooks/useChatbot.ts`):**

- Maneja estado de sesión
- Auto-reconnect
- Optimistic UI updates
- Error handling con retry
- Typing indicators
- Sound notifications

---

### PASO 7: Testing Local End-to-End ⏱️ ~2 horas

**Qué verificar:**

| Test              | Comando                                  | Esperado                |
| ----------------- | ---------------------------------------- | ----------------------- |
| LLM Server health | `curl localhost:8000/health`             | `{"status": "healthy"}` |
| Gateway routing   | `curl localhost:18443/api/chat/sessions` | 401 (sin token)         |
| Start session     | POST con JWT                             | Session ID              |
| Send message      | POST con "Busco una yipeta"              | Respuesta con vehículos |
| Dominican slang   | "Klk, tiene guagua pela'a?"              | Entiende y responde     |
| PII detection     | Enviar cédula "001-1234567-8"            | Respuesta sin cédula    |
| Lead scoring      | Enviar mensajes de compra urgente        | Score > 70              |
| Transfer          | Score alto → transferir                  | Briefing generado       |
| End session       | POST end                                 | Session cerrada         |
| Widget UI         | Abrir chat en browser                    | Chat funcional          |

**Flujo E2E completo:**

```
Browser (localhost:3000)
  → Click chat bubble
  → Widget opens
  → POST /api/chat/sessions (via Next.js rewrite)
  → Gateway (localhost:18443)
  → ChatbotService (localhost:5060)
  → LlmService → llm-server (localhost:8000)
  → Llama 3 genera respuesta
  → Response back to widget
```

---

### PASO 8: Limpiar Artefactos de Dialogflow ⏱️ ~30 min

**Archivos a eliminar:**

- `DialogflowService.cs.ELIMINATED`

**Código a renombrar (opcional, backward compatible):**

- `DialogflowIntentName` → mantener por compatibilidad DB
- Actualizar comentarios y Swagger descriptions

---

### PASO 9: Deploy a Producción (DOKS) ⏱️ ~2 horas

**Pasos:**

1. **Build y push imágenes Docker:**

   ```bash
   # ChatbotService
   docker build -t ghcr.io/gregorymorenoiem/okla-chatbotservice:latest ./backend/ChatbotService
   docker push ghcr.io/gregorymorenoiem/okla-chatbotservice:latest

   # LLM Server
   docker build -t ghcr.io/gregorymorenoiem/okla-llm-server:latest ./backend/ChatbotService/LlmServer
   docker push ghcr.io/gregorymorenoiem/okla-llm-server:latest
   ```

2. **Subir modelo a HuggingFace Hub:**

   ```bash
   huggingface-cli upload gregorymorenoiem/okla-llama3-8b-chatbot \
     okla-llama3-8b-chatbot.Q4_K_M.gguf
   ```

3. **Deploy K8s:**

   ```bash
   # LLM Server
   kubectl apply -f backend/ChatbotService/k8s/llm-server.yaml

   # Esperar que el Job descargue el modelo (~5-10 min)
   kubectl wait --for=condition=complete job/download-llm-model -n okla --timeout=600s

   # ChatbotService (en deployments.yaml existente)
   kubectl apply -f k8s/deployments.yaml

   # Actualizar Gateway ConfigMap
   kubectl delete configmap gateway-config -n okla
   kubectl create configmap gateway-config \
     --from-file=ocelot.json=backend/Gateway/Gateway.Api/ocelot.prod.json -n okla
   kubectl rollout restart deployment/gateway -n okla
   ```

4. **Verificar en producción:**
   ```bash
   kubectl get pods -n okla | grep -E "chatbot|llm"
   kubectl logs -f deployment/chatbotservice -n okla
   kubectl logs -f deployment/llm-server -n okla
   ```

---

### PASO 10: Monitoreo Post-Deploy (FASE 5) ⏱️ Continuo

**Setup inicial:**

1. Verificar métricas Prometheus: `kubectl port-forward svc/llm-server 8000:8000 -n okla` → `/metrics`
2. Importar dashboard Grafana desde `FASE_5_MEJORA_CONTINUA/monitoring/grafana_dashboard.json`
3. Configurar alertas:
   - Latencia > 10s → Warning
   - Error rate > 5% → Critical
   - Model not loaded → Critical

**Cadencia operativa:**
| Frecuencia | Acción |
|---|---|
| **Diaria** | Revisar logs y métricas básicas |
| **Semanal** | Evaluar calidad con `evaluation/evaluate_model.py` |
| **Semanal** | Analizar feedback con `feedback/feedback_collector.py` |
| **Mensual** | Detectar drift con `monitoring/drift_detector.py` |
| **Trimestral** | Re-entrenar modelo si métricas bajan |

---

## 📐 Arquitectura Final

```
┌─────────────────────────────────────────────────────────────────┐
│                        USUARIO (Browser)                        │
│                     https://okla.com.do                         │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                    FRONTEND (Next.js 14)                        │
│                                                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐  │
│  │  ChatWidget   │  │  useChatbot  │  │  chatbotService.ts   │  │
│  │  (flotante)   │→│  (hook)      │→│  (API client)        │  │
│  └──────────────┘  └──────────────┘  └──────────┬───────────┘  │
│                                                  │              │
│                    Next.js Rewrite: /api/* → gateway:8080       │
└──────────────────────────────────────────────────┬──────────────┘
                                                   │
                                                   ▼
┌─────────────────────────────────────────────────────────────────┐
│                    GATEWAY (Ocelot)                             │
│                    /api/chat/* → chatbotservice:8080            │
└──────────────────────────────────────────────────┬──────────────┘
                                                   │
                                                   ▼
┌─────────────────────────────────────────────────────────────────┐
│                 CHATBOT SERVICE (.NET 8)                        │
│                                                                 │
│  ┌──────────┐   ┌────────────┐   ┌──────────────────────────┐  │
│  │  PII     │   │ Quick      │   │    LlmService            │  │
│  │ Detection│→ │ Response   │→ │  (HTTP → llm-server)     │  │
│  │ (regex)  │   │ Check      │   │  + Legal Audit           │  │
│  └──────────┘   └────────────┘   │  + Lead Scoring          │  │
│                                   └────────────┬─────────────┘  │
│                                                │                │
│  ┌──────────────────────────────────────────────┘               │
│  │ PostgreSQL (sessions, messages, leads, config)               │
│  │ Redis (cache, rate limiting)                                 │
│  │ RabbitMQ (events: lead.created, session.ended)               │
└──┴──────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                    LLM SERVER (FastAPI)                         │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  llama-cpp-python                                        │   │
│  │  Model: okla-llama3-8b-chatbot.Q4_K_M.gguf (~4.7 GB)   │   │
│  │  Context: 2048 tokens                                    │   │
│  │  Quantization: Q4_K_M (4-bit)                           │   │
│  │  API: OpenAI-compatible /v1/chat/completions             │   │
│  │  Metrics: Prometheus /metrics                            │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

---

## ⏱️ Timeline Estimado

| Paso | Tarea                    |  Tiempo  | Dependencia |
| :--: | ------------------------ | :------: | :---------: |
|  1   | Entrenar modelo en Colab |   ~3h    | Dataset ✅  |
|  2   | Probar LLM Server local  |  ~30min  |   Paso 1    |
|  3   | Docker Compose principal |  ~15min  |      —      |
|  4   | Gateway routes (Ocelot)  |  ~20min  |      —      |
|  5   | Widget chat frontend     |  ~4-6h   |      —      |
|  6   | Servicio API frontend    |   ~2h    |      —      |
|  7   | Testing E2E local        |   ~2h    |  Pasos 1-6  |
|  8   | Limpiar Dialogflow       |  ~30min  |      —      |
|  9   | Deploy a DOKS            |   ~2h    |  Pasos 1-8  |
|  10  | Monitoreo post-deploy    | Continuo |   Paso 9    |

**Total estimado: ~15-17 horas** (2-3 días de trabajo)

---

## 💰 Costos Estimados

| Recurso                                   | Costo                     |
| ----------------------------------------- | ------------------------- |
| **Colab Pro** (para entrenar)             | $10/mes (o gratis con T4) |
| **Droplet 8GB** (para LLM Server en DOKS) | ~$48/mes (s-4vcpu-8gb)    |
| **Block Storage 10Gi** (para modelo GGUF) | ~$1/mes                   |
| **Total mensual adicional**               | ~$49/mes                  |

---

## 🔧 Requisitos Previos

- [ ] Cuenta Google Colab (con GPU T4 o superior)
- [ ] HuggingFace account + token (para descargar Llama 3)
- [ ] Meta Llama 3 access granted (solicitar en huggingface.co/meta-llama/Meta-Llama-3-8B-Instruct)
- [ ] Docker Desktop con al menos 10GB RAM disponible
- [ ] kubectl configurado para okla-cluster
- [ ] ghcr.io access configurado

---

## ❓ Decisiones Pendientes

| Decisión                     | Opciones                                                | Recomendación                                     |
| ---------------------------- | ------------------------------------------------------- | ------------------------------------------------- |
| **¿Cuándo mostrar el chat?** | Siempre / Solo en páginas de vehículos / Solo logged in | Solo en páginas de vehículos + dealer             |
| **¿Chat anónimo?**           | Sí (con límites) / Solo autenticados                    | Anónimo con límite de 5 mensajes → pedir registro |
| **¿Sonido de notificación?** | Sí / No                                                 | Sí, configurable                                  |
| **¿WhatsApp integration?**   | Sí (Fase futura) / No                                   | Fase futura via Twilio                            |
| **¿Multi-idioma?**           | Solo español / Español + inglés                         | Solo español (RD market)                          |

---

_Documento generado para el equipo de desarrollo OKLA — Febrero 2026_
