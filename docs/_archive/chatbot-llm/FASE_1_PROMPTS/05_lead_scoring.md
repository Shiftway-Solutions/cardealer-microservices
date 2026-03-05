# 📐 Prompt 05 — Calificación de Leads

> **Fase:** 1 — Diseño de Prompts  
> **Última actualización:** Febrero 15, 2026

---

## 1. Nombre y Rol

**Lead Scoring Prompt** — Analiza la conversación completa para asignar un score (0-100) y temperatura (Cold/Warm/Hot) al lead. Genera JSON compatible con la entidad `ChatLead` del backend. Se ejecuta periódicamente durante la conversación o al finalizarla.

---

## 2. Trigger

- **Cuándo se ejecuta:**
  - Cada 3-4 mensajes de la conversación (evaluación incremental)
  - Al finalizar la sesión (`EndSession`)
  - Cuando `leadSignals` acumula ≥2 señales positivas
- **Qué lo activa:** El `SendMessageCommandHandler` evalúa las señales acumuladas del `LlmResponse.leadSignals`.

---

## 3. ⚠️ DIRECTIVA DE REEMPLAZO

> El ChatbotService actual NO tiene calificación de leads automatizada. `ChatLead` existe como entidad pero solo se crea manualmente al transferir a agente. El nuevo sistema LLM debe calificar leads AUTOMÁTICAMENTE en cada interacción y actualizar el score progresivamente.

---

## 4. Variables Dinámicas Requeridas

| Variable                   | Fuente                          | Tipo   | Ejemplo                   |
| -------------------------- | ------------------------------- | ------ | ------------------------- |
| `{{conversation_history}}` | Últimos N mensajes de la sesión | JSON   | Array de mensajes         |
| `{{current_signals}}`      | Señales acumuladas              | JSON   | Señales detectadas        |
| `{{session_metadata}}`     | Datos de la sesión              | JSON   | Canal, duración, mensajes |
| `{{dealer_name}}`          | ChatbotConfiguration            | string | "Auto Toyota Dominicana"  |

---

## 5. Texto Completo del Prompt

```
═══════════════════════════════════════════
CALIFICACIÓN DE LEADS — CHATBOT OKLA
═══════════════════════════════════════════

Eres un especialista en calificación de leads para venta de vehículos en República Dominicana. Analiza la conversación completa y asigna un score y temperatura al lead.

CONVERSACIÓN A ANALIZAR:
{{conversation_history}}

SEÑALES DETECTADAS HASTA AHORA:
{{current_signals}}

METADATA DE LA SESIÓN:
{{session_metadata}}

═══════════════════════════════════════════
CRITERIOS DE CALIFICACIÓN
═══════════════════════════════════════════

### 🔥 HOT (Score 85-100) — Alta probabilidad de compra
Señales que suman puntos:
- +20: Menciona presupuesto específico ("tengo 2 millones", "mi presupuesto es...")
- +20: Solicita prueba de manejo / test drive
- +15: Pregunta por financiamiento o condiciones de crédito
- +15: Compara modelos específicos (señal de decisión activa)
- +10: Proporciona datos de contacto voluntariamente
- +10: Pregunta por documentación necesaria para comprar
- +10: Pregunta por proceso de trade-in con vehículo específico
- +10: Urgencia temporal ("lo necesito esta semana", "antes del viernes")
- +5:  Pregunta por colores/versiones disponibles (ya decidió modelo)

### 🟡 WARM (Score 50-84) — Interesado pero en evaluación
- +15: Preguntas detalladas sobre un vehículo específico
- +10: Pregunta por disponibilidad
- +10: Menciona que está "buscando" o "viendo opciones"
- +10: Pregunta por garantía o servicio postventa
- +5:  Interacción prolongada (>5 mensajes relevantes)
- +5:  Regresa después de una sesión anterior

### ❄️ COLD (Score 0-49) — Navegando / Explorando
- +5:  Preguntas generales sin especificar
- +5:  Solo saluda y pregunta horarios/ubicación
- +0:  No proporciona datos de contacto
- -5:  Dice explícitamente "solo estoy mirando"
- -10: Respuestas monosilábicas sin engagement

### MODIFICADORES DE CONTEXTO:
- Canal WhatsApp: +5 (mayor intención que web anónimo)
- Sesión fuera de horario laboral: +5 (busca activamente)
- Menciona marca/modelo específico: +10
- Menciona competidor ("vi uno más barato en X"): +10 (está comparando, near decision)
- Queja o insatisfacción: -15 (riesgo de pérdida)

═══════════════════════════════════════════
ACCIONES RECOMENDADAS POR TEMPERATURA
═══════════════════════════════════════════

- HOT (≥85): → "transfer_to_agent" (conectar con vendedor INMEDIATAMENTE)
- WARM (50-84): → "nurture" (seguir conversación, ofrecer más info, sugerir test drive)
- COLD (<50): → "monitor" (dejar que explore, ofrecer ayuda genérica)

═══════════════════════════════════════════
FORMATO DE RESPUESTA
═══════════════════════════════════════════

Responde con EXACTAMENTE este JSON:

{
  "score": 87,
  "temperature": "Hot",
  "signals": [
    "mentioned_budget_2000000_dop",
    "requested_test_drive",
    "asked_financing",
    "provided_phone_number",
    "specific_model_rav4_2024"
  ],
  "negative_signals": [],
  "recommended_action": "transfer_to_agent",
  "urgency": "high",
  "sentiment": "positive",
  "summary": "Cliente con presupuesto definido de RD$2,000,000, interesado específicamente en Toyota RAV4 2024, solicitó prueba de manejo y preguntó por financiamiento. Proporcionó teléfono voluntariamente. Lead altamente calificado — transferir a vendedor.",
  "vehicle_interest": {
    "primary": "Toyota RAV4 2024",
    "secondary": "Hyundai Tucson 2023",
    "vehicleType": "SUV",
    "budget": 2000000,
    "currency": "DOP"
  },
  "customer_profile": {
    "name": "Juan Pérez",
    "phone": "+18095551234",
    "email": null,
    "preferredContact": "whatsapp",
    "needs": "Vehículo familiar, eficiente en combustible"
  },
  "next_best_action": "Llamar al cliente dentro de las próximas 2 horas. Mencionar disponibilidad del RAV4 y opciones de financiamiento. Ofrecer test drive para este fin de semana."
}

REGLAS:
- "score": Número entero 0-100
- "temperature": "Hot" | "Warm" | "Cold"
- "signals": Lista de señales positivas detectadas (snake_case)
- "negative_signals": Lista de señales negativas (si las hay)
- "recommended_action": "transfer_to_agent" | "nurture" | "monitor" | "schedule_followup"
- "urgency": "high" | "medium" | "low"
- "sentiment": "very_positive" | "positive" | "neutral" | "negative" | "very_negative"
- "summary": Resumen de 2-3 oraciones para el agente humano
- "vehicle_interest": Vehículo(s) de interés, puede ser null si no especificó
- "customer_profile": Datos del cliente recopilados (solo los que proporcionó)
- "next_best_action": Recomendación específica para el vendedor humano
```

---

## 6. Ejemplo Real de Input/Output

### Input:

```json
{
  "conversation_history": [
    { "role": "user", "content": "Hola, busco una yipeta familiar" },
    { "role": "assistant", "content": "Te recomiendo la Tucson y la RAV4..." },
    {
      "role": "user",
      "content": "¿Cuánto cuesta la RAV4? Tengo como 2.5 millones"
    },
    { "role": "assistant", "content": "La RAV4 2024 está en RD$2,850,000*..." },
    {
      "role": "user",
      "content": "¿Tienen financiamiento? Me interesa hacer un test drive"
    },
    {
      "role": "assistant",
      "content": "Sí, ofrecemos opciones de financiamiento..."
    },
    {
      "role": "user",
      "content": "Perfecto, mi número es 809-555-1234, soy Juan Pérez"
    }
  ],
  "session_metadata": {
    "channel": "web",
    "messageCount": 7,
    "durationMinutes": 12
  }
}
```

### Output:

```json
{
  "score": 92,
  "temperature": "Hot",
  "signals": [
    "mentioned_budget_2500000_dop",
    "requested_test_drive",
    "asked_financing",
    "provided_phone_number",
    "provided_name",
    "specific_model_rav4_2024",
    "family_need_identified"
  ],
  "negative_signals": [],
  "recommended_action": "transfer_to_agent",
  "urgency": "high",
  "sentiment": "positive",
  "summary": "Lead HOT: Juan Pérez, presupuesto RD$2.5M, quiere RAV4 2024 para familia. Pidió test drive y financiamiento. Proporcionó teléfono voluntariamente. Transferir a vendedor inmediatamente.",
  "vehicle_interest": {
    "primary": "Toyota RAV4 2024",
    "secondary": null,
    "vehicleType": "SUV",
    "budget": 2500000,
    "currency": "DOP"
  },
  "customer_profile": {
    "name": "Juan Pérez",
    "phone": "+18095551234",
    "email": null,
    "preferredContact": "phone",
    "needs": "SUV familiar, eficiente en combustible"
  },
  "next_best_action": "Llamar a Juan Pérez al 809-555-1234 en los próximos 30 minutos. Confirmar disponibilidad de RAV4 2024, presentar opciones de financiamiento y agendar test drive para los próximos 2-3 días."
}
```

---

## 7. Notas de Implementación (.NET 8)

### Integración con entidad `ChatLead`:

```csharp
// Cuando score >= 85 → crear/actualizar ChatLead automáticamente

if (leadScore.Score >= 85)
{
    var lead = session.Lead ?? new ChatLead();
    lead.FullName = leadScore.CustomerProfile?.Name;
    lead.Phone = leadScore.CustomerProfile?.Phone;
    lead.Email = leadScore.CustomerProfile?.Email;
    lead.InterestedVehicleId = ResolveVehicleId(leadScore.VehicleInterest?.Primary);
    lead.Budget = leadScore.VehicleInterest?.Budget;
    lead.WantsFinancing = leadScore.Signals.Contains("asked_financing");
    lead.HasTradeIn = leadScore.Signals.Contains("mentioned_trade_in");
    lead.Status = LeadStatus.Qualified;
    lead.Temperature = Enum.Parse<LeadTemperature>(leadScore.Temperature);
    lead.QualificationScore = leadScore.Score;
    lead.Notes = leadScore.Summary;

    session.Lead = lead;
    session.LeadId = lead.Id;

    // Sugerir transferencia a agente
    if (leadScore.RecommendedAction == "transfer_to_agent")
    {
        // Activar prompt 06 — Transferencia a Humano
    }
}
```

### Evaluación incremental (cada N mensajes):

```csharp
// En SendMessageCommandHandler — después de cada respuesta:
if (session.MessageCount % 3 == 0 || HasStrongSignals(llmResponse.LeadSignals))
{
    var leadScore = await _llmService.ScoreLeadAsync(
        BuildConversationContext(session), ct);

    await UpdateLeadAsync(session, leadScore, ct);
}
```
