# 📐 Prompt 06 — Transferencia a Agente Humano

> **Fase:** 1 — Diseño de Prompts  
> **Última actualización:** Febrero 15, 2026

---

## 1. Nombre y Rol

**Human Transfer Prompt** — Determina cuándo escalar a un agente humano y genera un resumen completo del contexto de la conversación para que el agente pueda continuar sin que el cliente repita información.

---

## 2. Trigger

- **Cuándo se ejecuta:**
  - Lead score ≥ 85 (HOT)
  - El usuario solicita explícitamente hablar con una persona
  - El usuario presenta una queja o insatisfacción
  - El chatbot no puede resolver la consulta (3+ fallbacks consecutivos)
  - El usuario comparte datos de pago (transferencia inmediata por seguridad)
  - Consultas legales, financieras detalladas o médicas
- **Qué lo activa:** `suggestedAction == "TRANSFER_TO_AGENT"` o detección manual de triggers.

---

## 3. ⚠️ DIRECTIVA DE REEMPLAZO

> El ChatbotService actual tiene un `TransferToAgentCommandHandler` que simplemente marca la sesión como `TransferredToAgent` y crea un lead básico. El nuevo sistema LLM debe generar un **resumen inteligente** de la conversación completa para el agente, algo que Dialogflow NO hacía.

---

## 4. Variables Dinámicas Requeridas

| Variable                   | Fuente                          | Tipo   | Ejemplo                  |
| -------------------------- | ------------------------------- | ------ | ------------------------ |
| `{{conversation_history}}` | Todos los mensajes de la sesión | JSON   | Array de mensajes        |
| `{{lead_score}}`           | Resultado del Prompt 05         | JSON   | Score, temperatura       |
| `{{transfer_reason}}`      | Razón de la transferencia       | string | "hot_lead"               |
| `{{dealer_name}}`          | ChatbotConfiguration            | string | "Auto Toyota Dominicana" |
| `{{dealer_phone}}`         | ChatbotConfiguration            | string | "+1-809-555-0100"        |
| `{{session_metadata}}`     | Datos de la sesión              | JSON   | Canal, duración, etc.    |

---

## 5. Texto Completo del Prompt

```
═══════════════════════════════════════════
TRANSFERENCIA A AGENTE HUMANO — CHATBOT OKLA
═══════════════════════════════════════════

Se ha activado la transferencia a un agente humano. Genera un resumen completo de la conversación para que el agente pueda continuar SIN que el cliente repita información.

RAZÓN DE TRANSFERENCIA: {{transfer_reason}}

CONVERSACIÓN COMPLETA:
{{conversation_history}}

LEAD SCORE ACTUAL:
{{lead_score}}

METADATA DE SESIÓN:
{{session_metadata}}

═══════════════════════════════════════════
INSTRUCCIONES
═══════════════════════════════════════════

1. GENERA un resumen ejecutivo de la conversación (máximo 5 oraciones).
2. EXTRAE todos los datos del cliente mencionados.
3. IDENTIFICA el vehículo de interés principal (y secundarios si los hay).
4. DESCRIBE la necesidad del cliente en una frase.
5. INDICA la urgencia y sentimiento general.
6. RECOMIENDA la acción específica que el agente debe tomar.

TAMBIÉN genera el mensaje de despedida para el cliente que verá ANTES de ser transferido.

═══════════════════════════════════════════
MENSAJES DE TRANSFERENCIA SEGÚN RAZÓN
═══════════════════════════════════════════

- hot_lead: "¡Excelente! Te voy a conectar con uno de nuestros asesores de ventas que podrá atenderte de forma personalizada. Un momento, por favor. 🤝"

- customer_request: "¡Con gusto! Te transfiero con un miembro de nuestro equipo que podrá ayudarte directamente. Un momento. 🤝"

- complaint: "Lamento los inconvenientes. Te transfiero con un especialista de nuestro equipo que podrá resolver tu situación. Un momento, por favor. 🙏"

- unresolved: "Disculpa que no pueda ayudarte con eso directamente. Te voy a conectar con uno de nuestros asesores que tiene toda la información necesaria. 🤝"

- payment_data: "Por tu seguridad, necesito transferirte con un asesor que pueda gestionar este proceso de forma segura. Un momento. 🔒"

- legal_financial: "Esa consulta requiere atención especializada. Te conecto con un asesor que puede darte la información precisa. 🤝"

═══════════════════════════════════════════
FORMATO DE RESPUESTA
═══════════════════════════════════════════

{
  "action": "TRANSFER_TO_AGENT",
  "transfer_message": "[Mensaje de despedida al cliente según razón]",
  "reason": "hot_lead | customer_request | complaint | unresolved | payment_data | legal_financial",
  "priority": "urgent | high | normal",
  "agent_briefing": {
    "summary": "Resumen ejecutivo de 3-5 oraciones para el agente",
    "customer": {
      "name": "Juan Pérez",
      "phone": "+18095551234",
      "email": null,
      "preferredContact": "whatsapp",
      "language": "es"
    },
    "vehicle_interest": {
      "primary": "Toyota RAV4 2024 — RD$2,850,000",
      "secondary": "Hyundai Tucson 2023 — RD$1,950,000",
      "vehicleType": "SUV",
      "budget": "RD$2,500,000",
      "condition": "Nuevo o seminuevo"
    },
    "needs": "Vehículo familiar SUV, eficiente en combustible",
    "financing": {
      "interested": true,
      "details": "Preguntó por opciones de financiamiento, no especificó inicial"
    },
    "tradeIn": {
      "interested": false,
      "currentVehicle": null
    },
    "conversation_highlights": [
      "Tiene presupuesto definido de RD$2.5M",
      "Pidió test drive del RAV4",
      "Interesado en financiamiento",
      "Tono positivo y decidido"
    ],
    "sentiment": "positive",
    "urgency": "high",
    "lead_score": 92,
    "lead_temperature": "Hot",
    "session_stats": {
      "messages": 8,
      "duration_minutes": 12,
      "channel": "web"
    }
  },
  "recommended_agent_action": "Llamar dentro de los próximos 30 minutos. Tener disponibilidad del RAV4 confirmada y opciones de financiamiento listas. Ofrecer test drive para este fin de semana."
}
```

---

## 6. Ejemplo Real de Input/Output

### Input:

Lead score 92, razón: hot_lead, conversación de 8 mensajes sobre RAV4.

### Output:

```json
{
  "action": "TRANSFER_TO_AGENT",
  "transfer_message": "¡Excelente, Juan! Te voy a conectar con uno de nuestros asesores de ventas que podrá atenderte de forma personalizada con el RAV4 2024. Un momento, por favor. 🤝",
  "reason": "hot_lead",
  "priority": "urgent",
  "agent_briefing": {
    "summary": "Juan Pérez busca SUV familiar, presupuesto RD$2.5M. Muy interesado en Toyota RAV4 2024, pidió test drive y preguntó por financiamiento. Proporcionó teléfono voluntariamente. Lead altamente calificado, actuar rápido.",
    "customer": {
      "name": "Juan Pérez",
      "phone": "+18095551234",
      "email": null,
      "preferredContact": "phone",
      "language": "es"
    },
    "vehicle_interest": {
      "primary": "Toyota RAV4 2024 — RD$2,850,000",
      "secondary": null,
      "vehicleType": "SUV",
      "budget": "RD$2,500,000",
      "condition": "Nuevo"
    },
    "needs": "SUV familiar eficiente en combustible",
    "financing": {
      "interested": true,
      "details": "Preguntó sobre opciones, sin detalles específicos"
    },
    "tradeIn": { "interested": false, "currentVehicle": null },
    "conversation_highlights": [
      "Presupuesto definido: RD$2.5M",
      "Solicitó test drive",
      "Preguntó por financiamiento",
      "Proporcionó datos de contacto voluntariamente"
    ],
    "sentiment": "positive",
    "urgency": "high",
    "lead_score": 92,
    "lead_temperature": "Hot",
    "session_stats": { "messages": 8, "duration_minutes": 12, "channel": "web" }
  },
  "recommended_agent_action": "Llamar a Juan Pérez al 809-555-1234 en máximo 30 minutos. Confirmar disponibilidad del RAV4 2024. Preparar 2-3 opciones de financiamiento. Ofrecer test drive este fin de semana."
}
```

---

## 7. Notas de Implementación (.NET 8)

### Modificar `TransferToAgentCommandHandler`:

```csharp
// ANTES (Dialogflow — solo marcaba la sesión):
session.Status = SessionStatus.TransferredToAgent;

// DESPUÉS (LLM — genera resumen inteligente):
var transferResult = await _llmService.GenerateTransferBriefingAsync(
    conversationHistory, leadScore, transferReason, ct);

session.Status = SessionStatus.TransferredToAgent;

// Guardar briefing para el agente
var lead = session.Lead ?? new ChatLead();
lead.Notes = transferResult.AgentBriefing.Summary;
lead.Temperature = Enum.Parse<LeadTemperature>(transferResult.AgentBriefing.LeadTemperature);
lead.QualificationScore = transferResult.AgentBriefing.LeadScore;

// Enviar notificación al dealer con briefing
await _notificationClient.SendAsync(new NotificationRequest
{
    Channel = "whatsapp",
    To = config.DealerPhone,
    Template = "agent_transfer",
    Data = new
    {
        customerName = transferResult.AgentBriefing.Customer.Name,
        customerPhone = transferResult.AgentBriefing.Customer.Phone,
        summary = transferResult.AgentBriefing.Summary,
        priority = transferResult.Priority,
        recommendedAction = transferResult.RecommendedAgentAction
    }
});

// Auditar transferencia
await _auditClient.LogActionAsync(new AuditLogRequest
{
    Action = "CHAT_TRANSFER_TO_AGENT",
    EntityType = "ChatSession",
    EntityId = session.Id.ToString(),
    Details = JsonSerializer.Serialize(transferResult)
});
```
