# 📐 FASE 1 — Sistema Modular de Prompts para el Chatbot LLM de OKLA

> **Última actualización:** Febrero 17, 2026  
> **Versión:** 2.0 — Arquitectura Dual-Mode (SingleVehicle + DealerInventory)

---

## ✅ Estado: Implementación Completada

> El chatbot LLM basado en Llama 3.1 8B (fine-tuned con QLoRA) **reemplaza completamente** a Google Dialogflow ES. La arquitectura usa el **Strategy Pattern** con `ChatModeRouter` para enrutar cada sesión al modo correcto.

### Modos de Chat

| Modo                     | Trigger             | Estrategia                | Contexto                              |
| ------------------------ | ------------------- | ------------------------- | ------------------------------------- |
| **SingleVehicle (SV)**   | `vehicleId != null` | `SingleVehicleStrategy`   | 1 vehículo fijo (~500 tokens)         |
| **DealerInventory (DI)** | `dealerId != null`  | `DealerInventoryStrategy` | RAG top-5 vía pgvector (~1500 tokens) |
| **General**              | Ambos `null`        | Prompt estático           | Sin inventario (~200 tokens)          |

### Intents por Modo

| Modo        | Intents                           | Exclusivos                                                            |
| ----------- | --------------------------------- | --------------------------------------------------------------------- |
| **SV**      | 21                                | `VehicleNotInInventory` (boundary)                                    |
| **DI**      | 23                                | `VehicleSearch`, `VehicleComparison`, `CrossDealerRefusal` (boundary) |
| **General** | Subset de SV sin vehicle-specific |

---

## 📋 Índice de Prompts

| #   | Prompt                                                    | Archivo                        | Rol                                        | Ejecución                               |
| --- | --------------------------------------------------------- | ------------------------------ | ------------------------------------------ | --------------------------------------- |
| 01  | [System Prompt Base](01_system_prompt_base.md)            | `01_system_prompt_base.md`     | Personalidad, contexto, límites legales    | Cada conversación (system message)      |
| 02  | [Consulta de Inventario](02_inventory_query.md)           | `02_inventory_query.md`        | Búsqueda de vehículos con filtros          | Cuando intent = VehicleSearch           |
| 03  | [Agendamiento de Citas](03_appointment_scheduling.md)     | `03_appointment_scheduling.md` | Protocolo paso a paso para citas           | Cuando intent = TestDrive/Appointment   |
| 04  | [Auditoría Legal](04_legal_audit.md)                      | `04_legal_audit.md`            | Verificación pre-envío de cada respuesta   | Después de cada respuesta del LLM       |
| 05  | [Calificación de Leads](05_lead_scoring.md)               | `05_lead_scoring.md`           | Score 0-100 y temperatura Cold/Warm/Hot    | Cada 3 mensajes o señales fuertes       |
| 06  | [Transferencia a Humano](06_human_transfer.md)            | `06_human_transfer.md`         | Resumen inteligente para el agente         | Cuando score ≥ 85 o solicitud explícita |
| 07  | [Análisis de Conversaciones](07_conversation_analysis.md) | `07_conversation_analysis.md`  | Fine-tuning candidates, fallback analysis  | CRON semanal (domingos 2AM)             |
| 08  | [Manejo de Objeciones](08_objection_handling.md)          | `08_objection_handling.md`     | Objeciones de precio, competencia, dudas   | Cuando intent = Negotiation             |
| 09  | [Comparación de Vehículos](09_vehicle_comparison.md)      | `09_vehicle_comparison.md`     | Tabla comparativa 2-3 vehículos            | Cuando intent = VehicleComparison       |
| 10  | [Detección de PII](10_pii_detection.md)                   | `10_pii_detection.md`          | Protección de datos sensibles (Ley 172-13) | Cada mensaje (pre y post LLM)           |

---

## 🔄 Pipeline Completo (Dual-Mode)

```
┌──────────────────────────────────────────────────────────────┐
│                    USUARIO ENVÍA MENSAJE                      │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  PASO 1: DETECCIÓN DE PII (Prompt 10)                        │
│  → Regex en backend (NO enviar PII al LLM)                   │
│  → Si tarjeta de crédito → TRANSFER inmediato                │
│  → Si otro PII → enmascarar antes de enviar al LLM           │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  PASO 2: QUICK RESPONSE CHECK                                │
│  → Buscar en QuickResponse por keywords                      │
│  → Si match → responder inmediatamente ($0, sin LLM)         │
└──────────────┬───────────────────────┬───────────────────────┘
               │ NO MATCH              │ MATCH → Respuesta
               ▼                       │
┌──────────────────────────────────────────────────────────────┐
│  PASO 2.5: ChatModeRouter — ENRUTAMIENTO POR MODO            │
│                                                              │
│  vehicleId != null  → SingleVehicleStrategy                  │
│  dealerId  != null  → DealerInventoryStrategy                │
│  ambos null         → GeneralPrompt (estático)               │
│                                                              │
│  Strategy.BuildSystemPromptAsync() → System Prompt + Context │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  PASO 3: CONSTRUIR CONTEXTO LLM                              │
│  → System Prompt por modo (Prompt 01 — SV/DI/GEN)            │
│  → SV: Datos fijos de 1 vehículo (~500 tokens)               │
│  → DI: RAG top-5 vía pgvector (~1500 tokens)                 │
│  → + Citas si agendamiento (03)                              │
│  → + Objeciones si negociación (08, mode-aware)              │
│  → + Comparación si DI + 2+ vehículos (09)                   │
│  → + Historial (últimos 10 msgs)                             │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  PASO 4: LLAMADA AL LLM + GBNF Grammar                      │
│  → POST a API (RunPod / Ollama)                              │
│  → GBNF fuerza JSON de 8 campos                              │
│  → Timeout: 10s, Retry: 3x exponencial, Circuit Breaker: 5  │
│  → Parsear JSON con: response, intent, confidence,           │
│    isFallback, parameters, leadSignals, suggestedAction,     │
│    quickReplies                                              │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  PASO 4.5: BOUNDARY ENFORCEMENT                              │
│  → SV: ¿Menciona otro vehículo? → Redirect cortés           │
│  → DI: ¿Menciona otro dealer? → CrossDealerRefusal           │
│  → Grounding: ¿Inventó datos? → Sanitize                    │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  PASO 5: AUDITORÍA LEGAL (Prompt 04)                         │
│  → Verificar cumplimiento con 4 leyes RD                     │
│  → APPROVED → enviar tal cual                                │
│  → NEEDS_REVISION → enviar versión corregida                 │
│  → BLOCKED → enviar mensaje genérico + transferir a agente   │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  PASO 6: LEAD SCORING (Prompt 05)                            │
│  → Evaluar cada 3 mensajes o señales fuertes                 │
│  → Si score ≥ 85 → activar transferencia (Prompt 06)         │
│  → Actualizar ChatLead automáticamente                       │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│  PASO 7: RESPUESTA AL USUARIO                                │
│  → Guardar mensaje en PostgreSQL (PII enmascarado)           │
│  → Actualizar InteractionUsage (costo)                       │
│  → Retornar respuesta con metadata                           │
└──────────────────────────────────────────────────────────────┘

                   ═══════════════
                   PROCESO BATCH
                   ═══════════════

┌──────────────────────────────────────────────────────────────┐
│  CRON SEMANAL: ANÁLISIS DE CONVERSACIONES (Prompt 07)        │
│  → Domingos 2AM                                              │
│  → Analizar por modo (SV vs DI metrics separadas)            │
│  → Generar candidatos de fine-tuning                         │
│  → Sugerir nuevas Quick Responses                            │
│  → Calcular métricas de calidad                              │
└──────────────────────────────────────────────────────────────┘
```

---

## 📊 Formato de Respuesta — GBNF Schema (8 campos)

Todas las respuestas del LLM son forzadas a este JSON vía **GBNF grammar**:

```json
{
  "response": "Texto de la respuesta al usuario",
  "intent": "VehiclePrice",
  "confidence": 0.95,
  "isFallback": false,
  "parameters": { "vehicleId": "abc-123" },
  "leadSignals": {
    "interested": true,
    "readyToBuy": false,
    "wantsTestDrive": false,
    "mentionedBudget": false,
    "askedFinancing": false,
    "providedContactInfo": false
  },
  "suggestedAction": "show_vehicle_card",
  "quickReplies": ["Ver detalles", "Financiamiento", "Agendar prueba"]
}
```

### Valores válidos para `suggestedAction`:

| Valor                    | Cuándo                         |
| ------------------------ | ------------------------------ |
| `null`                   | Sin acción especial            |
| `"show_vehicle_card"`    | Menciona vehículo específico   |
| `"TRANSFER_TO_AGENT"`    | Lead HOT o solicitud explícita |
| `"SCHEDULE_APPOINTMENT"` | Cita confirmada                |
| `"search_inventory"`     | DI: búsqueda de inventario     |
| `"compare_vehicles"`     | DI: comparación                |
| `"SCORE_LEAD"`           | Forzar evaluación inmediata    |

---

## 🏗️ Archivos del Backend (Implementados ✅)

### Domain Layer

- ✅ `Domain/Interfaces/ILlmService.cs`, `IChatModeStrategy.cs`
- ✅ `Domain/Models/LlmModels.cs` — LlmRequest, LlmResponse, GbnfSchema
- ✅ `Domain/Entities/ChatbotConfiguration.cs` — Campos LLM
- ✅ `Domain/Enums/ChatMode.cs` — SingleVehicle, DealerInventory, General

### Application Layer

- ✅ `Application/Services/ChatModeRouter.cs` — Strategy Pattern routing
- ✅ `Application/Services/PromptInjectionDetector.cs`
- ✅ `Application/Services/PiiDetector.cs`
- ✅ `Application/Services/OutputGroundingValidator.cs`
- ✅ `Application/Services/ContentModerationFilter.cs`

### Infrastructure Layer

- ✅ `Infrastructure/Services/LlmService.cs` — Llama 3.1 8B inference
- ✅ `Infrastructure/Strategies/SingleVehicleStrategy.cs`
- ✅ `Infrastructure/Strategies/DealerInventoryStrategy.cs`
- ✅ `Infrastructure/Services/RagSearchService.cs` — pgvector
