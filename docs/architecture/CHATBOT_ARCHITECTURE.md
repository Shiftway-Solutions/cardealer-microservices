# 🤖 Arquitectura del Chatbot — OKLA

**Última actualización:** Febrero 2026  
**Servicio:** ChatbotService (.NET 8, Clean Architecture)  
**Modelo:** `gregorymorenoiem/okla-chatbot-llama3-8b` (Llama 3 8B fine-tuned con QLoRA)  
**Nombre del Bot:** "Ana" / "OKLA Assistant"

---

## 📋 Tabla de Contenidos

1. [Resumen de Arquitectura](#resumen-de-arquitectura)
2. [Arquitectura Anterior vs. Nueva](#arquitectura-anterior-vs-nueva)
3. [Proveedores Externos (HuggingFace)](#proveedores-externos-huggingface)
4. [Pipeline de Procesamiento de Mensajes](#pipeline-de-procesamiento-de-mensajes)
5. [Sistema RAG (Retrieval-Augmented Generation)](#sistema-rag-retrieval-augmented-generation)
6. [Estrategias de Chat (Strategy Pattern)](#estrategias-de-chat-strategy-pattern)
7. [Quick Responses (Respuestas Rápidas)](#quick-responses-respuestas-rápidas)
8. [Configuración y Secrets](#configuración-y-secrets)
9. [Costos y Presupuesto de Tokens](#costos-y-presupuesto-de-tokens)
10. [Guía de Despliegue](#guía-de-despliegue)
11. [Troubleshooting](#troubleshooting)

---

## Resumen de Arquitectura

El chatbot de OKLA es un asistente de ventas automotrices que opera en español dominicano. Procesa mensajes de usuarios a través de un pipeline de 12 pasos que incluye detección de inyección de prompts, enmascaramiento de PII, respuestas rápidas (sin IA), búsqueda semántica de inventario (RAG), e inferencia LLM.

### Componentes Principales

| Componente         | Tecnología                         | Función                                  |
| ------------------ | ---------------------------------- | ---------------------------------------- |
| **ChatbotService** | .NET 8 (CQRS + MediatR)            | Orquesta el pipeline completo            |
| **LLM Inference**  | HuggingFace Inference Endpoints    | Genera respuestas en lenguaje natural    |
| **Embeddings**     | HuggingFace Inference API (gratis) | Genera vectores para búsqueda semántica  |
| **Vector DB**      | PostgreSQL + pgvector              | Almacena y busca embeddings de vehículos |
| **Cache**          | Redis                              | Cache de sesiones y respuestas LLM       |
| **Message Broker** | RabbitMQ                           | Eventos de auditoría y notificaciones    |

---

## Arquitectura Anterior vs. Nueva

### ❌ Arquitectura Anterior (Self-Hosted — DEPRECADA)

```
┌──────────────┐     ┌───────────────────┐     ┌──────────────────────────────┐
│   Frontend   │────▶│  ChatbotService   │────▶│     llm-server (Pod K8s)     │
│   (Browser)  │     │   (.NET 8)        │     │  Python FastAPI              │
│              │◀────│                   │◀────│  + llama-cpp-python          │
└──────────────┘     │                   │     │  + sentence-transformers     │
                     │   PostgreSQL ◀────│     │  Modelo: Llama 3 8B (GGUF)  │
                     │   (pgvector)      │     │  Embedding: MiniLM-L6-v2    │
                     └───────────────────┘     └──────────────────────────────┘
                                                        ⬆ PROBLEMA:
                                                   Requiere 8-10 GB RAM
                                                   Solo 4-5 GB libres en cluster
                                                   Inferencia CPU: 2-5 minutos
```

**Problemas:**

- El modelo Llama 3 8B (GGUF Q4_K_M) requiere ~5 GB de RAM solo para cargarse
- La inferencia en CPU toma 2-5 minutos por mensaje
- El cluster DOKS tiene ~4-5 GB libres por nodo — insuficiente
- El `llm-server` y el embedding model vivían en el **mismo contenedor**
- Si llm-server no estaba desplegado → TANTO chat como RAG fallaban

### ✅ Arquitectura Nueva (Proveedores Externos)

```
┌──────────────┐     ┌───────────────────┐     ┌─────────────────────────────────┐
│   Frontend   │────▶│  ChatbotService   │────▶│  HuggingFace Inference Endpoint │
│   (Browser)  │     │   (.NET 8)        │     │  (GPU dedicada, auto-sleep)     │
│              │◀────│                   │◀────│  Modelo: okla-chatbot-llama3-8b │
└──────────────┘     │                   │     │  API: /v1/chat/completions      │
                     │                   │     └─────────────────────────────────┘
                     │                   │
                     │                   │     ┌─────────────────────────────────┐
                     │                   │────▶│  HuggingFace Inference API      │
                     │                   │◀────│  (GRATIS, rate-limited)         │
                     │   PostgreSQL ◀────│     │  Modelo: all-MiniLM-L6-v2      │
                     │   (pgvector)      │     │  API: /models/{model}           │
                     └───────────────────┘     └─────────────────────────────────┘
```

**Ventajas:**

- **Chat (LLM):** GPU dedicada en HuggingFace → respuestas en 1-3 segundos (vs 2-5 min en CPU)
- **Embeddings:** API gratuita de HuggingFace → costo $0 para generación de vectores
- **Auto-sleep:** El endpoint se duerme después de N minutos sin uso → paga solo por uso real
- **Desacoplamiento:** LLM y embeddings son servicios independientes → si uno falla, el otro sigue
- **Cero carga en cluster:** No se necesita RAM ni CPU del cluster para inferencia

### Separación de Responsabilidades

| Componente             | Antes                                                 | Ahora                                         |
| ---------------------- | ----------------------------------------------------- | --------------------------------------------- |
| **Chat LLM**           | llm-server:8000/v1/chat/completions (CPU, 2-5 min)    | HuggingFace Inference Endpoint (GPU, 1-3 seg) |
| **Embeddings**         | llm-server:8000/v1/embeddings (CPU, mismo pod)        | HuggingFace Inference API gratuita (separado) |
| **Datos de vehículos** | ChatbotService consulta PostgreSQL, inyecta en prompt | Sin cambio — idéntico                         |
| **RAG (búsqueda)**     | pgvector cosine similarity                            | Sin cambio — idéntico                         |

> ⚠️ **IMPORTANTE:** El modelo LLM **NUNCA** accede a la base de datos directamente.  
> ChatbotService consulta PostgreSQL, selecciona los 5 vehículos más relevantes, y los inyecta en el system prompt como contexto. El modelo solo recibe texto y genera texto.

---

## Proveedores Externos (HuggingFace)

### LLM — HuggingFace Inference Endpoints (Pagado)

| Propiedad          | Valor                                      |
| ------------------ | ------------------------------------------ |
| **Proveedor**      | HuggingFace Inference Endpoints            |
| **Modelo**         | `gregorymorenoiem/okla-chatbot-llama3-8b`  |
| **API format**     | OpenAI-compatible (`/v1/chat/completions`) |
| **GPU**            | NVIDIA T4 (16 GB VRAM) o similar           |
| **Costo estimado** | ~$0.60/hora GPU × horas activas            |
| **Auto-sleep**     | Sí — se duerme tras N minutos sin tráfico  |
| **Cold start**     | 30-60 segundos al despertar                |
| **Latencia**       | 1-3 segundos por respuesta                 |

**Formato de Request:**

```json
{
  "model": "okla-chatbot-llama3-8b",
  "messages": [
    { "role": "system", "content": "Eres OKLA Assistant..." },
    { "role": "user", "content": "Tienes yipetas disponibles?" }
  ],
  "temperature": 0.3,
  "max_tokens": 400,
  "top_p": 0.9,
  "repetition_penalty": 1.15,
  "stop": ["</s>", "<|eot_id|>"]
}
```

**Formato de Response:**

```json
{
  "choices": [
    {
      "message": {
        "role": "assistant",
        "content": "{\"intent\": \"search_vehicle\", \"response\": \"¡Claro! Tenemos varias yipetas disponibles...\"}"
      }
    }
  ]
}
```

### Embeddings — HuggingFace Inference API (Gratis)

| Propiedad         | Valor                                            |
| ----------------- | ------------------------------------------------ |
| **Proveedor**     | HuggingFace Inference API (gratuita)             |
| **URL base**      | `https://api-inference.huggingface.co`           |
| **Endpoint**      | `/models/sentence-transformers/all-MiniLM-L6-v2` |
| **Dimensiones**   | 384                                              |
| **Costo**         | $0 (gratuito con rate limit)                     |
| **Rate limit**    | ~30,000 requests/día (con API token)             |
| **Autenticación** | Bearer token (mismo HF API token)                |

**Formato de Request (HuggingFace):**

```json
{
  "inputs": [
    "Toyota Corolla 2024 Sedán Gasolina Automática",
    "Busco yipeta diesel"
  ]
}
```

**Formato de Response (HuggingFace):**

```json
[
  [0.034, -0.021, 0.089, ...],  // 384 floats
  [0.012, 0.045, -0.033, ...]   // 384 floats
]
```

> **Nota:** El formato HuggingFace es diferente al formato OpenAI. El `EmbeddingService` detecta el proveedor via `Embedding:Provider` y usa el formato correcto automáticamente.

---

## Pipeline de Procesamiento de Mensajes

Cada mensaje del usuario pasa por un pipeline de **12 pasos** orquestado por `SendMessageCommandHandler`:

```
    Mensaje del Usuario
          │
          ▼
   ┌──────────────────┐
   │ 1. Cargar Sesión  │ ← Carga ChatSession + DealerChatConfig
   │    + Configuración │
   └──────┬───────────┘
          ▼
   ┌──────────────────┐
   │ 2. Handoff Check  │──→ Si humano controla → guardar mensaje, no procesar
   └──────┬───────────┘
          ▼
   ┌──────────────────┐
   │ 3. Límite de      │──→ Si excedió interacciones → "contacta un agente"
   │    Interacciones  │
   └──────┬───────────┘
          ▼
   ┌──────────────────┐
   │ 4. Detección de   │──→ Inyección detectada → respuesta segura
   │    Prompt Injection│──→ Sospechoso → sanitizar texto
   └──────┬───────────┘
          ▼
   ┌──────────────────┐
   │ 5. Detección PII  │──→ Datos sensibles (tarjeta) → bloquear
   │                   │──→ PII menor → enmascarar
   └──────┬───────────┘
          ▼
   ┌──────────────────┐
   │ 6. Quick Response │──→ ¿Match por keyword? SÍ → respuesta predefinida
   │    (sin IA)       │                             (costo $0)
   └──────┬───────────┘
          │ NO (necesita IA)
          ▼
   ┌──────────────────┐
   │ 7. Strategy       │ ← Factory resuelve por ChatMode:
   │    Pattern        │   • SingleVehicle → contexto fijo de 1 vehículo
   │    (RAG + Prompt) │   • DealerInventory → RAG pgvector (top-5)
   │                   │   • General → prompt genérico marketplace
   └──────┬───────────┘
          ▼
   ┌──────────────────┐
   │ 8. LLM Inference  │ ← HuggingFace Inference Endpoint
   │    (LlmService)   │   POST /v1/chat/completions
   │                   │   Authorization: Bearer hf_xxx
   │                   │   Circuit breaker: 10 fallos → 2 min abierto
   └──────┬───────────┘
          ▼
   ┌──────────────────┐
   │ 9. Sanitizar PII  │ ← Evita que el LLM repita PII del usuario
   │    en Respuesta   │
   └──────┬───────────┘
          ▼
   ┌──────────────────┐
   │ 10. Grounding     │ ← Anti-alucinación: verifica que la respuesta
   │     Validation    │   sea coherente con el contexto proporcionado
   └──────┬───────────┘
          ▼
   ┌──────────────────┐
   │ 11. Incrementar   │ ← InteractionCount++, check límite
   │     Contadores    │
   └──────┬───────────┘
          ▼
   ┌──────────────────┐
   │ 12. Persistir     │ ← Guarda ChatMessage (user + bot)
   │     Mensajes      │   Actualiza UpdatedAt de sesión
   └──────────────────┘
          │
          ▼
    ChatbotResponse
    al Frontend
```

### Resiliencia (Polly)

| Policy              | Configuración                      | Razón                                                                  |
| ------------------- | ---------------------------------- | ---------------------------------------------------------------------- |
| **Retry**           | `Policy.NoOpAsync()` (sin retries) | LLM inference puede tomar tiempo; reintentar envía requests duplicados |
| **Circuit Breaker** | 10 fallos → abierto por 2 minutos  | Si el endpoint está caído, no saturar con requests                     |

### Token Budget Management

```
Context Window = 4096 tokens
├── System Prompt:       ~200-500 tokens (varía por estrategia)
├── RAG Context:         ~163 tokens (5 vehículos × ~32 tokens c/u)
├── Historial (6 msgs):  ~200-400 tokens
├── Mensaje usuario:     ~20-50 tokens
└── Reservado output:    400 tokens (MaxTokens)
    ─────────────────
    Total prompt:        ~783-1,113 tokens (máx ~3,696)
```

Si el prompt excede el presupuesto, se recortan mensajes del historial (se mantienen el system prompt y el mensaje actual del usuario).

---

## Sistema RAG (Retrieval-Augmented Generation)

### Visión General

El RAG permite al chatbot responder preguntas sobre el inventario de vehículos de un dealer. El proceso tiene **3 pasos**:

```
┌────────────────────┐    ┌─────────────────────────┐    ┌────────────────────┐
│  1. Extracción de  │    │  2. Generación de       │    │  3. Búsqueda       │
│     Filtros        │───▶│     Embedding del Query  │───▶│     Híbrida        │
│  (Regex/C#)        │    │  (HuggingFace API)       │    │  (pgvector)        │
│  NO necesita LLM   │    │  384 dimensiones         │    │  cosine similarity │
└────────────────────┘    └─────────────────────────┘    │  + filtros SQL     │
                                                         └────────┬───────────┘
                                                                  │ Top-5 resultados
                                                                  ▼
                                                         ┌────────────────────┐
                                                         │  Inyectar en       │
                                                         │  System Prompt     │
                                                         │  como contexto     │
                                                         └────────────────────┘
```

### Paso 1: Extracción de Filtros (C# — Sin IA)

El `DealerInventoryStrategy` usa regex para extraer filtros estructurados del texto del usuario. Detecta español dominicano:

| Filtro              | Ejemplos de Input                       | Valor Extraído                                |
| ------------------- | --------------------------------------- | --------------------------------------------- |
| **Transmisión**     | "automática", "manual", "estándar"      | `Automatic` / `Manual`                        |
| **Combustible**     | "diesel", "eléctrico", "híbrido", "gas" | `Diesel` / `Electric` / `Hybrid` / `Gasoline` |
| **Tipo de cuerpo**  | "yipeta", "SUV", "pickup", "sedán"      | `SUV` / `Pickup` / `Sedan`                    |
| **Rango de precio** | "menos de 1.5M", "500 mil a 1 millón"   | `PriceMin/PriceMax`                           |

> **"Yipeta"** es el término dominicano para SUV. El sistema mapea automáticamente variantes locales.

### Paso 2: Generación de Embeddings

El texto del query del usuario se convierte en un vector de 384 dimensiones usando el modelo `all-MiniLM-L6-v2` via HuggingFace Inference API (gratuita).

### Paso 3: Búsqueda Híbrida (pgvector)

```sql
SELECT
    vehicle_id, content, metadata,
    1 - (embedding <=> @queryEmbedding::vector) AS similarity
FROM vehicle_embeddings
WHERE dealer_id = @dealerId
  AND (metadata->>'IsAvailable')::boolean = true
  -- Filtros dinámicos del Paso 1:
  AND LOWER(metadata->>'Make') = LOWER(@make)           -- si detectado
  AND (metadata->>'Price')::decimal >= @priceMin         -- si detectado
  AND (metadata->>'Price')::decimal <= @priceMax         -- si detectado
  AND LOWER(metadata->>'FuelType') = LOWER(@fuelType)   -- si detectado
  AND LOWER(metadata->>'Transmission') = LOWER(@trans)  -- si detectado
  AND LOWER(metadata->>'BodyType') = LOWER(@bodyType)   -- si detectado
ORDER BY embedding <=> @queryEmbedding::vector
LIMIT 5
```

### Tabla de Embeddings

```sql
CREATE TABLE IF NOT EXISTS vehicle_embeddings (
    id UUID PRIMARY KEY,
    vehicle_id UUID NOT NULL,
    dealer_id UUID NOT NULL,
    content TEXT NOT NULL,
    embedding vector(384),
    metadata JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_ve_dealer ON vehicle_embeddings (dealer_id);
CREATE INDEX idx_ve_vehicle ON vehicle_embeddings (vehicle_id);
CREATE UNIQUE INDEX idx_ve_dealer_vehicle ON vehicle_embeddings (dealer_id, vehicle_id);
-- IVFFlat index creado automáticamente cuando count >= 100
```

### Contenido del Embedding

Cada vehículo se convierte a texto optimizado para búsqueda antes de generar su embedding:

```
"{Year} {Make} {Model} {BodyType} {FuelType} {Transmission} {ExteriorColor}
 Precio: {Price:N0} {Currency} Kilometraje: {Mileage:N0}km {Condition}
 {Features joined by space}"
```

**Ejemplo:**

```
"2024 Toyota Corolla Sedán Gasolina Automática Blanco
 Precio: 1,500,000 DOP Kilometraje: 15,000km Usado
 Cámara reversa Bluetooth Pantalla táctil"
```

---

## Estrategias de Chat (Strategy Pattern)

El sistema usa el patrón Strategy + Factory para construir prompts especializados según el modo de chat:

### ChatMode → Strategy Mapping

| ChatMode          | Strategy Class                | RAG | Function Calling | Caso de Uso                                   |
| ----------------- | ----------------------------- | :-: | :--------------: | --------------------------------------------- |
| `SingleVehicle`   | `SingleVehicleChatStrategy`   | ❌  |        ❌        | Usuario pregunta sobre UN vehículo específico |
| `DealerInventory` | `DealerInventoryChatStrategy` | ✅  | ✅ (4 funciones) | Usuario busca en inventario del dealer        |
| `General`         | `GeneralChatStrategy`         | ❌  |        ❌        | Conversación general sobre marketplace        |

### Funciones del DealerInventory Strategy

El modo `DealerInventory` le describe 4 funciones al LLM que el modelo puede "llamar" (output JSON que ChatbotService interpreta):

| Función                | Parámetros                                                                     | Descripción                               |
| ---------------------- | ------------------------------------------------------------------------------ | ----------------------------------------- |
| `search_inventory`     | make, model, year_min, year_max, price_min, price_max, fuel_type, transmission | Buscar vehículos en inventario            |
| `compare_vehicles`     | vehicle_ids[]                                                                  | Comparar 2+ vehículos lado a lado         |
| `schedule_appointment` | vehicle_id, preferred_date, preferred_time                                     | Agendar test drive                        |
| `get_vehicle_details`  | vehicle_id                                                                     | Obtener detalles completos de un vehículo |

### Grounding Validation

| Estrategia          | Validación                                                            |
| ------------------- | --------------------------------------------------------------------- |
| **SingleVehicle**   | Verifica que el LLM no confunda marca/modelo del vehículo en contexto |
| **DealerInventory** | Detecta patrones de lenguaje especulativo ("podría ser", "tal vez")   |
| **General**         | Siempre retorna `grounded=true` (sin validación)                      |

---

## Quick Responses (Respuestas Rápidas)

Antes de invocar al LLM ($$$), el sistema intenta responder con **plantillas predefinidas** (costo $0):

### Funcionamiento

1. El `QuickResponseService` busca en la tabla `QuickResponses` por `DealerId`
2. Compara las `Keywords` del template contra el mensaje del usuario (case-insensitive)
3. Si hay match → retorna la `ResponseText` directamente, **sin llamar al LLM**
4. Se ordena por `Priority` (mayor = se evalúa primero)

### Templates Seeded

| Dealer                  | Categoría        | Keywords                                                | Ejemplo de Respuesta                                        |
| ----------------------- | ---------------- | ------------------------------------------------------- | ----------------------------------------------------------- |
| Auto Dominicana Premium | `horario`        | "horario", "hora", "abierto", "cerrado", "cuando abren" | "Nuestro horario es Lun-Vie 8:30AM-6PM..."                  |
| Auto Dominicana Premium | `financiamiento` | "financiamiento", "credito", "cuotas", "inicial"        | "Ofrecemos financiamiento con las principales entidades..." |
| Auto Dominicana Premium | `ubicacion`      | "donde estan", "direccion", "ubicacion", "mapa"         | "Estamos ubicados en Av. 27 de Febrero..."                  |
| MotorMax RD             | `horario`        | "horario", "hora", "abierto"                            | "Tamo abierto de Lunes a Sábado..." (tono dominicano)       |
| MotorMax RD             | `financiamiento` | "financiamiento", "credito", "cuotas"                   | "Te montamos con el financiamiento..." (tono dominicano)    |
| MotorMax RD             | `ubicacion`      | "donde estan", "direccion"                              | "Tamo en la Av. Lincoln..."                                 |

> **Nota:** Los dealers pueden agregar más templates via la API REST (`POST /api/chatbot/quick-responses`). Los templates son personalizables por dealer, permitiendo tono y contenido diferentes.

---

## Configuración y Secrets

### appsettings.json (valores por defecto para desarrollo)

```json
{
  "LlmService": {
    "ServerUrl": "http://llm-server:8000",
    "ModelId": "okla-llama3-8b",
    "LanguageCode": "es",
    "TimeoutSeconds": 60,
    "Temperature": 0.3,
    "TopP": 0.9,
    "MaxTokens": 400,
    "RepetitionPenalty": 1.15,
    "CompletionsPath": "/v1/chat/completions",
    "ApiKey": ""
  },
  "Embedding": {
    "ServerUrl": "http://llm-server:8000",
    "Model": "all-MiniLM-L6-v2",
    "Dimensions": 384,
    "TimeoutSeconds": 30,
    "EmbeddingsPath": "/v1/embeddings",
    "ApiKey": "",
    "Provider": "openai"
  }
}
```

### K8s Secret — `chatbot-llm-secrets`

En producción/staging, los valores se inyectan via Kubernetes Secret:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: chatbot-llm-secrets
  namespace: okla
type: Opaque
stringData:
  # ── Chat LLM (HuggingFace Inference Endpoints) ──
  LlmService__ServerUrl: "https://xxxx.endpoints.huggingface.cloud"
  LlmService__ApiKey: "hf_xxxxxxxxxxxxxxxxxxxx"
  LlmService__ModelId: "okla-chatbot-llama3-8b"
  LlmService__CompletionsPath: "/v1/chat/completions"
  # ── Embeddings (HuggingFace Inference API — gratis) ──
  Embedding__ServerUrl: "https://api-inference.huggingface.co"
  Embedding__ApiKey: "hf_xxxxxxxxxxxxxxxxxxxx"
  Embedding__EmbeddingsPath: "/models/sentence-transformers/all-MiniLM-L6-v2"
  Embedding__Provider: "huggingface"
```

### Variables de Entorno (deployments.yaml)

| Variable                      | Fuente                | Descripción                                   |
| ----------------------------- | --------------------- | --------------------------------------------- |
| `LlmService__ServerUrl`       | `chatbot-llm-secrets` | URL del HF Inference Endpoint                 |
| `LlmService__ApiKey`          | `chatbot-llm-secrets` | HuggingFace API Token                         |
| `LlmService__ModelId`         | `chatbot-llm-secrets` | ID del modelo en el endpoint                  |
| `LlmService__CompletionsPath` | `chatbot-llm-secrets` | Ruta de la API (`/v1/chat/completions`)       |
| `Embedding__ServerUrl`        | `chatbot-llm-secrets` | URL de la API de embeddings de HF             |
| `Embedding__ApiKey`           | `chatbot-llm-secrets` | Mismo HF API Token                            |
| `Embedding__EmbeddingsPath`   | `chatbot-llm-secrets` | Ruta del modelo de embeddings                 |
| `Embedding__Provider`         | `chatbot-llm-secrets` | `"huggingface"` (formato de request distinto) |

### Soporte Multi-Proveedor

La arquitectura soporta **cualquier proveedor** compatible con la API de OpenAI:

| Proveedor                    | `ServerUrl`                                | `ApiKey`  | `CompletionsPath`      | `Provider` (embeddings) |
| ---------------------------- | ------------------------------------------ | --------- | ---------------------- | ----------------------- |
| **HuggingFace Endpoints**    | `https://xxxx.endpoints.huggingface.cloud` | `hf_xxx`  | `/v1/chat/completions` | N/A                     |
| **HuggingFace API (gratis)** | N/A                                        | N/A       | N/A                    | `huggingface`           |
| **OpenAI**                   | `https://api.openai.com`                   | `sk-xxx`  | `/v1/chat/completions` | `openai`                |
| **Groq**                     | `https://api.groq.com/openai`              | `gsk_xxx` | `/v1/chat/completions` | `openai`                |
| **Together AI**              | `https://api.together.xyz`                 | `xxx`     | `/v1/chat/completions` | `openai`                |
| **Self-hosted (llm-server)** | `http://llm-server:8000`                   | _(vacío)_ | `/v1/chat/completions` | `openai`                |

> Si `ApiKey` está vacío, no se envía header `Authorization`. Esto permite seguir usando el llm-server self-hosted sin cambios.

---

## Costos y Presupuesto de Tokens

### Costo por Interacción (HuggingFace Inference Endpoints)

| Componente                | Tokens                  | Costo               |
| ------------------------- | ----------------------- | ------------------- |
| System prompt             | ~200-500                | Incluido en compute |
| RAG context (5 vehículos) | ~163                    | Incluido en compute |
| Historial (6 mensajes)    | ~200-400                | Incluido en compute |
| Mensaje usuario           | ~20-50                  | Incluido en compute |
| **Respuesta LLM**         | **~400 (max)**          | Incluido en compute |
| **Embeddings (HF API)**   | N/A                     | **$0 (gratis)**     |
| **TOTAL por mensaje**     | **~1,000-1,500 tokens** | **~$0.0003-0.001**  |

> HuggingFace Inference Endpoints cobra por **hora de compute**, no por token. Con auto-sleep, el costo real depende de cuántas horas al día el endpoint está activo.

### Estimación Mensual

| Escenario                                | Horas GPU/mes | Costo/mes |
| ---------------------------------------- | ------------- | --------- |
| **Bajo tráfico** (1-2 horas activas/día) | ~45h          | ~$27      |
| **Medio tráfico** (4-6 horas/día)        | ~150h         | ~$90      |
| **Alto tráfico** (24/7 siempre activo)   | ~720h         | ~$432     |

> **Embeddings: siempre $0** (HuggingFace Inference API gratuita, ~30,000 requests/día).

### Optimización de Costos

1. **Quick Responses** → Respuestas predefinidas para preguntas frecuentes (horario, ubicación, financiamiento). Costo: $0.
2. **Auto-sleep** → Endpoint se duerme después de N minutos sin tráfico (configurable en HuggingFace).
3. **Circuit Breaker** → Si el endpoint falla 10 veces, para de enviar requests por 2 minutos.
4. **Token Budget** → El prompt nunca excede 3,696 tokens (4096 - 400 de output).
5. **RAG limit** → Solo 5 vehículos por query, independientemente del tamaño del inventario del dealer.

---

## Guía de Despliegue

### Paso 1: Crear HuggingFace Inference Endpoint

1. Ir a [HuggingFace Inference Endpoints](https://ui.endpoints.huggingface.co/)
2. Crear nuevo endpoint con:
   - **Model:** `gregorymorenoiem/okla-chatbot-llama3-8b`
   - **Task:** Text Generation
   - **GPU:** NVIDIA T4 (más económica) o L4
   - **Region:** us-east-1 (más cercano a NYC1 del cluster DOKS)
   - **Framework:** Text Generation Inference (TGI)
   - **Auto-sleep:** Activar (ej. 15 minutos sin tráfico)
3. Copiar la URL del endpoint (ej. `https://xxxx.endpoints.huggingface.cloud`)

### Paso 2: Obtener API Token

1. Ir a [HuggingFace Settings → Access Tokens](https://huggingface.co/settings/tokens)
2. Crear token con permisos `read` (mínimo) o `inference` (recomendado)
3. Copiar el token (formato `hf_xxxxxxxxxxxx`)

### Paso 3: Crear K8s Secret

```bash
# Crear el secret con los valores reales
kubectl create secret generic chatbot-llm-secrets \
  --namespace=okla \
  --from-literal=LlmService__ServerUrl="https://YOUR-ENDPOINT.endpoints.huggingface.cloud" \
  --from-literal=LlmService__ApiKey="hf_YOUR_TOKEN_HERE" \
  --from-literal=LlmService__ModelId="okla-chatbot-llama3-8b" \
  --from-literal=LlmService__CompletionsPath="/v1/chat/completions" \
  --from-literal=Embedding__ServerUrl="https://api-inference.huggingface.co" \
  --from-literal=Embedding__ApiKey="hf_YOUR_TOKEN_HERE" \
  --from-literal=Embedding__EmbeddingsPath="/models/sentence-transformers/all-MiniLM-L6-v2" \
  --from-literal=Embedding__Provider="huggingface"
```

O usando el template:

```bash
export HF_API_TOKEN="hf_YOUR_TOKEN_HERE"
export HF_LLM_ENDPOINT_URL="https://YOUR-ENDPOINT.endpoints.huggingface.cloud"
export HF_LLM_MODEL_ID="okla-chatbot-llama3-8b"
envsubst < k8s/secrets.template.yaml | kubectl apply -f -
```

### Paso 4: Rebuild y Deploy ChatbotService

```bash
# Rebuild la imagen Docker (push a GHCR via CI/CD)
git add -A && git commit -m "feat(chatbot): add HuggingFace external provider support" && git push

# O reiniciar el deployment para que tome el nuevo secret
kubectl rollout restart deployment/chatbotservice -n okla
```

### Paso 5: Verificar

```bash
# Ver logs del chatbot
kubectl logs -f deployment/chatbotservice -n okla

# Test directo al endpoint de chat
kubectl exec -it deployment/chatbotservice -n okla -- \
  curl -s http://localhost:8080/api/chatbot/health | jq .

# Port-forward para probar desde local
kubectl port-forward svc/chatbotservice 8080:8080 -n okla
curl -X POST http://localhost:8080/api/chatbot/sessions/start \
  -H "Content-Type: application/json" \
  -d '{"dealerId": "...", "userId": "..."}'
```

---

## Troubleshooting

### "Estamos experimentando dificultades técnicas"

**Causa:** El LLM no está accesible (endpoint apagado, token inválido, circuit breaker abierto).

**Diagnóstico:**

```bash
# Ver logs recientes
kubectl logs deployment/chatbotservice -n okla --tail=50

# Buscar errores de LLM
kubectl logs deployment/chatbotservice -n okla | grep -i "llm\|circuit\|huggingface"
```

**Posibles causas:**
| Síntoma en logs | Causa | Solución |
|---|---|---|
| `HttpRequestException: 401 Unauthorized` | API token inválido o expirado | Regenerar token en HuggingFace y actualizar secret |
| `HttpRequestException: 503 Service Unavailable` | Endpoint en auto-sleep (cold start) | Esperar 30-60 segundos, el endpoint se reactiva automáticamente |
| `TaskCanceledException: timeout` | Inferencia tarda más que `TimeoutSeconds` | Aumentar timeout o verificar GPU del endpoint |
| `LLM circuit breaker opened` | 10+ fallos consecutivos | Esperar 2 minutos o verificar estado del endpoint |
| `Failed to generate embeddings` | HuggingFace Inference API caída | Verificar status.huggingface.co; embeddings son gratis con rate limit |

### Cold Start del Endpoint

Cuando el endpoint está en auto-sleep, la primera petición tarda 30-60 segundos en responder. El `TimeoutSeconds` default es 60, suficiente para cubrir el cold start. Si los usuarios experimentan timeout en la primera interacción:

1. Aumentar `LlmService:TimeoutSeconds` a 120 en el secret
2. O desactivar auto-sleep en HuggingFace (costo mayor)
3. O implementar un health check que haga "warm-up" del endpoint periódicamente

### Cambiar de Proveedor

Para cambiar de HuggingFace a otro proveedor (Groq, Together AI, OpenAI):

```bash
# Actualizar el secret con el nuevo proveedor
kubectl delete secret chatbot-llm-secrets -n okla
kubectl create secret generic chatbot-llm-secrets \
  --namespace=okla \
  --from-literal=LlmService__ServerUrl="https://api.groq.com/openai" \
  --from-literal=LlmService__ApiKey="gsk_YOUR_GROQ_KEY" \
  --from-literal=LlmService__ModelId="llama-3.1-8b-instant" \
  --from-literal=LlmService__CompletionsPath="/v1/chat/completions" \
  --from-literal=Embedding__ServerUrl="https://api-inference.huggingface.co" \
  --from-literal=Embedding__ApiKey="hf_YOUR_TOKEN" \
  --from-literal=Embedding__EmbeddingsPath="/models/sentence-transformers/all-MiniLM-L6-v2" \
  --from-literal=Embedding__Provider="huggingface"

# Reiniciar para tomar cambios
kubectl rollout restart deployment/chatbotservice -n okla
```

> ⚠️ **Nota:** Si cambias a un modelo diferente al fine-tuned (`okla-chatbot-llama3-8b`), las respuestas perderán el tono dominicano especializado y el formato JSON entrenado.

---

## Archivos Modificados

| Archivo                                                         | Cambio                                                                                                                                        |
| --------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| `ChatbotService.Infrastructure/Services/LlmService.cs`          | Agregado `ApiKey`, `CompletionsPath` a `LlmSettings`. HTTP request ahora usa `HttpRequestMessage` con header `Authorization: Bearer` opcional |
| `ChatbotService.Infrastructure/Services/VectorSearchService.cs` | `EmbeddingSettings` tiene `ApiKey`, `Provider`, `EmbeddingsPath`. Soporte dual: formato OpenAI y formato HuggingFace para embeddings          |
| `ChatbotService.Infrastructure/DependencyInjection.cs`          | Registrado HttpClient `"EmbeddingServer"` (faltaba — bug). URL y timeout separados del LlmServer                                              |
| `ChatbotService.Api/appsettings.json`                           | Nuevos campos: `CompletionsPath`, `ApiKey` en LlmService; `EmbeddingsPath`, `ApiKey`, `Provider` en Embedding                                 |
| `k8s/deployments.yaml`                                          | 8 env vars nuevos en chatbotservice leyendo de `chatbot-llm-secrets`                                                                          |
| `k8s/secrets.template.yaml`                                     | Nuevo secret `chatbot-llm-secrets` con variables para LLM y embeddings                                                                        |

---

_Documento generado — Febrero 2026_
