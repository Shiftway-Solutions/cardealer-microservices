# 🤖 OKLA AI-Powered Marketplace Engine — Arquitectura

**Versión:** 2.0  
**Fecha:** Febrero 17, 2026  
**Estado:** Rediseño completo para Dual-Mode Chat + WhatsApp

---

## 📋 Resumen Ejecutivo

OKLA evoluciona de un chatbot simple a un **AI-powered marketplace engine** con dos modos contextuales de chat, integración WhatsApp bidireccional, RAG con búsqueda vectorial, y handoff bot↔humano.

---

## 🎯 Escenarios de Chat

### 🔵 Escenario 1 — Single Vehicle Mode

- Usuario ve un vehículo específico y abre chat
- Bot solo responde sobre ESE vehículo
- Contexto fijo: datos exactos de la publicación
- Capacidades: precio, financiamiento, ubicación, garantía, agendar cita

### 🟢 Escenario 2 — Dealer Inventory Mode

- Usuario entra al perfil del dealer y abre chat
- Bot responde sobre CUALQUIER vehículo del dealer
- Capacidades: buscar, comparar, recomendar, filtrar, agendar cita
- Búsqueda semántica + filtros estructurados (RAG)

### 📱 WhatsApp Integration

- Dealer puede activar bot en su WhatsApp Business
- Bot responde mensajes entrantes automáticamente
- Handoff bot↔humano (dealer toma control y devuelve al bot)
- Mismo engine, adaptado a formato WhatsApp

---

## 🏗️ Arquitectura de Alto Nivel

```
                    ┌────────────────────────────────────┐
                    │           CLIENTES                  │
                    │                                    │
                    │  ┌──────────┐  ┌──────────────┐   │
                    │  │ Web Chat │  │ WhatsApp API │   │
                    │  │ Widget   │  │ (Meta/Twilio) │   │
                    │  └────┬─────┘  └──────┬───────┘   │
                    └───────┼───────────────┼────────────┘
                            │               │
                    ┌───────▼───────────────▼────────────┐
                    │        API Gateway (Ocelot)         │
                    └───────┬───────────────┬────────────┘
                            │               │
              ┌─────────────▼─┐    ┌────────▼─────────────┐
              │ ChatController│    │WhatsAppController    │
              │ /api/chat/*   │    │/api/whatsapp/webhook │
              └───────┬───────┘    └────────┬─────────────┘
                      │                     │
              ┌───────▼─────────────────────▼─────────────┐
              │              MediatR Pipeline               │
              │                                            │
              │  ┌──────────────────────────────────────┐  │
              │  │         ChatModeRouter                │  │
              │  │                                      │  │
              │  │  vehicleId? ──▶ SingleVehicleStrategy │  │
              │  │  dealerId?  ──▶ DealerInventoryStrategy│ │
              │  └──────────────────────────────────────┘  │
              │                                            │
              │  ┌──────────────┐  ┌────────────────────┐  │
              │  │ Security     │  │ RAG Pipeline       │  │
              │  │ Pipeline     │  │                    │  │
              │  │ - Injection  │  │ - pgvector search  │  │
              │  │ - PII detect │  │ - Hybrid filters   │  │
              │  │ - Rate limit │  │ - Context builder  │  │
              │  └──────────────┘  └────────────────────┘  │
              └──────────┬─────────────────────────────────┘
                         │
              ┌──────────▼──────────────────────────────────┐
              │           LLM Inference Layer                 │
              │                                              │
              │  ┌────────────────────────────────────────┐  │
              │  │  LlmServer (FastAPI + llama-cpp-python) │  │
              │  │  Llama 3.1 8B fine-tuned (GGUF Q4_K_M) │  │
              │  │  + Function Calling via GBNF Grammar    │  │
              │  │  + Extended Context (8192 tokens)        │  │
              │  └────────────────────────────────────────┘  │
              └──────────────────────────────────────────────┘

              ┌──────────────────────────────────────────────┐
              │              Data Layer                       │
              │                                              │
              │  ┌────────────┐  ┌──────────┐  ┌─────────┐  │
              │  │ PostgreSQL │  │ pgvector │  │  Redis  │  │
              │  │ (EF Core)  │  │ (RAG)    │  │ (Cache) │  │
              │  └────────────┘  └──────────┘  └─────────┘  │
              └──────────────────────────────────────────────┘
```

---

## 🧠 Strategy Pattern — Dual Mode

### IChatModeStrategy Interface

```csharp
public interface IChatModeStrategy
{
    ChatMode Mode { get; }
    Task<string> BuildContextAsync(ChatSession session, string userMessage, CancellationToken ct);
    Task<string> BuildSystemPromptAsync(ChatSession session, ChatbotConfiguration config, CancellationToken ct);
    Task<List<FunctionDefinition>> GetAvailableFunctionsAsync(ChatSession session, CancellationToken ct);
}
```

### SingleVehicleStrategy

- Lookup directo por `VehicleId`
- System prompt con datos fijos del vehículo
- Sin function calling (no necesita buscar)
- Contexto: ~500 tokens

### DealerInventoryStrategy

- RAG con pgvector para búsqueda semántica
- Function calling: `search_inventory`, `compare_vehicles`, `schedule_appointment`
- System prompt con capacidades del dealer
- Contexto: dinámico según consulta (~2,000-4,000 tokens)

---

## 📚 RAG Pipeline (pgvector)

```
Vehicle CRUD Events ──▶ EmbeddingWorker ──▶ pgvector
     │                      │                   │
     │  VehicleCreated      │  Generate text     │  vector(384)
     │  VehicleUpdated      │  → Embed (local)   │  + metadata JSONB
     │  VehicleDeleted      │  → Upsert          │  + dealer_id filter
     │  VehicleSold         │                   │
                            │                   │
User Query ────────────▶ QueryEmbedding ────▶ Hybrid Search
                            │                   │
                            │  Embed query       │  Semantic (cosine)
                            │  + Extract filters │  + SQL filters
                            │                   │  (price, year, etc.)
                            │                   │
                            │              Top-K Results (3-5)
                            │                   │
                            ▼                   ▼
                     LLM Generation with Retrieved Context
```

### Embedding Model

- **all-MiniLM-L6-v2** (384 dims, local, gratuito)
- Corre como sidecar en el LlmServer
- ~20ms por embedding

### Hybrid Search

Combina búsqueda semántica (cosine similarity) con filtros SQL:

```sql
SELECT * FROM vehicle_embeddings
WHERE dealer_id = @dealerId
  AND (metadata->>'price')::decimal <= @maxPrice
  AND (metadata->>'transmission') = @transmission
ORDER BY embedding <=> @queryEmbedding
LIMIT 5;
```

---

## 📱 WhatsApp Integration

### Arquitectura

```
Meta Cloud API ──webhook──▶ WhatsAppController
                                │
                           Validate signature
                           Parse message type
                                │
                    ┌───────────┼───────────┐
                    ▼           ▼           ▼
               Text msg    Media msg    Status update
                    │           │           │
                    ▼           ▼           │
             Route to      Store media     │
             ChatEngine    + process       │
                    │           │           │
                    ▼           ▼           │
              ┌─────────────────────┐      │
              │  Session Manager    │      │
              │                     │      │
              │  Is Bot mode?       │      │
              │  ├─ Yes → LLM       │      │
              │  └─ No  → Queue for │      │
              │          human agent │      │
              └──────────┬──────────┘      │
                         │                 │
                    Send response          │
                    via Meta API           │
```

### Handoff Flow

```
Bot Mode ──(dealer clicks "Take Over")──▶ Human Mode
     │                                         │
     │  Bot responds automatically            │  Messages forwarded
     │  via LLM engine                        │  to dealer dashboard
     │                                         │  Dealer types replies
     │                                         │
     │◀──(dealer clicks "Return to Bot")──────│
```

---

## 🔐 AI Safety Layers

| Layer | Component                  | Purpose                                   |
| ----- | -------------------------- | ----------------------------------------- |
| 1     | `PromptInjectionDetector`  | Block system prompt manipulation          |
| 2     | `PiiDetector`              | Redact cédulas, tarjetas, datos sensibles |
| 3     | `WhatsAppMessageValidator` | Rate limit, country filter, blacklist     |
| 4     | `OutputGroundingValidator` | Verify LLM only mentions real inventory   |
| 5     | `MultiTenantIsolation`     | Strict dealer_id filtering in all queries |
| 6     | `ContentModerationFilter`  | Block offensive/inappropriate content     |

---

## 🔧 Infraestructura

### LlmServer Upgrades

- **Contexto extendido**: 4096 → 8192 tokens (Llama 3.1 soporta hasta 128K)
- **Function calling**: GBNF grammar extendida con `function_call` field
- **Embedding endpoint**: `/v1/embeddings` usando sentence-transformers
- **Batch processing**: Para embeddings masivos durante sync

### Docker Services

| Service        | Puerto    | Propósito                |
| -------------- | --------- | ------------------------ |
| chatbotservice | 5060/8080 | .NET 8 API               |
| llm-server     | 8000      | Llama 3.1 + Embeddings   |
| chatbot-db     | 5434      | PostgreSQL 16 + pgvector |
| chatbot-redis  | 6380      | Response cache           |

### Kubernetes

- ConfigMap para system prompts y configuración
- Secret para WhatsApp tokens y API keys
- HPA basado en CPU/memory para el chatbot service
- GPU node pool opcional para LLM (o CPU con paciencia)

---

## 📊 Métricas y Observabilidad

### Prometheus Metrics (nuevas)

- `chatbot_mode_usage{mode="single_vehicle|dealer_inventory"}` — Uso por modo
- `chatbot_rag_search_duration_seconds` — Latencia de búsqueda vectorial
- `chatbot_whatsapp_messages_total{direction="inbound|outbound"}` — Tráfico WhatsApp
- `chatbot_handoff_total{direction="bot_to_human|human_to_bot"}` — Handoffs
- `chatbot_embedding_generation_seconds` — Tiempo de generación de embeddings
- `chatbot_function_calls_total{function="search|compare|schedule"}` — Function calling

---

## 📋 Etapas de Implementación

### Etapa 1 — Foundation

- [x] Strategy Pattern (SingleVehicle + DealerInventory)
- [x] ChatMode enum y routing
- [x] pgvector schema y embedding pipeline
- [x] System prompts diferenciados por modo
- [x] Domain entities updates

### Etapa 2 — RAG & Intelligence

- [x] Hybrid search (semántico + filtros SQL)
- [x] Function calling definitions
- [x] Vehicle embedding worker
- [x] Extended context window (8192)

### Etapa 3 — WhatsApp Integration

- [x] WhatsApp webhook controller
- [x] Inbound message processing
- [x] Bot ↔ Human handoff
- [x] WhatsApp-specific prompt adaptations
- [x] Rate limiting per phone number

### Etapa 4 — AI Safety

- [x] Output grounding validation
- [x] Multi-tenant isolation enforcement
- [x] WhatsApp-specific security
- [x] Content moderation

### Etapa 5 — Infrastructure

- [x] LlmServer upgrades (embeddings, extended context, function calling)
- [x] Docker Compose updates
- [x] Gateway route updates

---

_Documento generado automáticamente — Febrero 2026_
