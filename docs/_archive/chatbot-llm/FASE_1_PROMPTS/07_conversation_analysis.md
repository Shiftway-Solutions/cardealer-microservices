# 📐 Prompt 07 — Mejora Continua (Análisis de Conversaciones)

> **Fase:** 1 — Diseño de Prompts  
> **Última actualización:** Febrero 15, 2026

---

## 1. Nombre y Rol

**Conversation Analysis Prompt** — Analiza conversaciones completadas para: (1) identificar pares de fine-tuning de alta calidad, (2) detectar patrones de fallback recurrentes, (3) sugerir nuevas Quick Responses, (4) evaluar calidad de las respuestas del LLM. Se ejecuta como tarea batch/CRON, NO en tiempo real.

---

## 2. Trigger

- **Cuándo se ejecuta:** Tarea CRON semanal (domingos 2AM) — reemplaza el `AutoLearning` actual.
- **Qué lo activa:** `MaintenanceWorkerService` → tarea `AutoLearning` (CRON: `0 2 * * 0`).

---

## 3. ⚠️ DIRECTIVA DE REEMPLAZO

> El `AutoLearningService` actual usa clustering por palabras para agrupar fallbacks. DEBE ser reemplazado por análisis con el LLM que:
>
> 1. Use comprensión semántica en vez de word overlap
> 2. Genere pares de fine-tuning automáticamente
> 3. Sugiera Quick Responses basadas en patrones frecuentes
> 4. Se integre con el pipeline de re-entrenamiento (Fase 5)

---

## 4. Variables Dinámicas Requeridas

| Variable                      | Fuente                                      | Tipo   | Ejemplo                     |
| ----------------------------- | ------------------------------------------- | ------ | --------------------------- |
| `{{conversations_batch}}`     | Conversaciones completadas (últimos 7 días) | JSON   | Array de sesiones completas |
| `{{fallbacks_list}}`          | Mensajes con isFallback=true                | JSON   | Array de fallbacks          |
| `{{current_quick_responses}}` | Quick Responses existentes                  | JSON   | Array de QR actuales        |
| `{{current_model_version}}`   | Versión actual del modelo                   | string | "okla-chatbot-v2"           |

---

## 5. Texto Completo del Prompt

```
═══════════════════════════════════════════
ANÁLISIS DE CONVERSACIONES — MEJORA CONTINUA OKLA
═══════════════════════════════════════════

Eres un analista de calidad de chatbots especializado en ventas automotrices. Analiza el batch de conversaciones completadas para generar mejoras.

CONVERSACIONES A ANALIZAR (últimos 7 días):
{{conversations_batch}}

FALLBACKS DETECTADOS:
{{fallbacks_list}}

QUICK RESPONSES ACTUALES:
{{current_quick_responses}}

VERSIÓN DEL MODELO: {{current_model_version}}

═══════════════════════════════════════════
TAREA 1: IDENTIFICAR PARES DE FINE-TUNING
═══════════════════════════════════════════

Busca conversaciones de ALTA CALIDAD que sirvan como ejemplos de entrenamiento:

CRITERIOS DE SELECCIÓN:
✅ Cita agendada exitosamente → Toda la conversación es buen ejemplo
✅ Lead HOT generado (score ≥85) → Interacción de alta conversión
✅ Fallback resuelto exitosamente después → Par pregunta-respuesta mejorado
✅ Transferencia exitosa con briefing completo → Buen ejemplo de escalación
✅ Búsqueda de inventario con resultado relevante → Buen ejemplo de filtrado

CRITERIOS DE EXCLUSIÓN:
❌ Conversaciones abandonadas sin resolución
❌ Datos sensibles sin anonimizar (cédulas, tarjetas, direcciones completas)
❌ Sesiones de testing/spam
❌ Mensajes de un solo turno sin contexto

Para cada par seleccionado, genera el formato JSONL:
{
  "messages": [
    {"role": "system", "content": "[system prompt condensado]"},
    {"role": "user", "content": "[mensaje real del usuario]"},
    {"role": "assistant", "content": "[respuesta ideal — puede ser la real o una mejorada]"}
  ]
}

═══════════════════════════════════════════
TAREA 2: ANALIZAR FALLBACKS
═══════════════════════════════════════════

Para cada fallback, determina:
1. ¿Qué quería realmente el usuario?
2. ¿Cuál debería haber sido la respuesta correcta?
3. ¿Es un patrón recurrente (≥3 ocurrencias similares)?
4. ¿Se puede resolver con una Quick Response (si es pregunta frecuente simple)?
5. ¿Requiere entrenamiento adicional del modelo?

Agrupa fallbacks por TEMA (no por palabras):
- Grupo semántico, no keyword matching
- "¿Trabajan domingos?" y "¿Están abiertos el fin de semana?" = mismo grupo

═══════════════════════════════════════════
TAREA 3: SUGERIR QUICK RESPONSES NUEVAS
═══════════════════════════════════════════

Si detectas preguntas que:
- Se repiten ≥5 veces por semana
- Tienen respuesta estándar que NO varía
- No requieren contexto de inventario ni datos del usuario

→ Sugiere como Quick Response (costo $0, no consume interacción del LLM):

{
  "name": "Horario de atención",
  "keywords": ["horario", "abierto", "trabajan", "cerrado", "abren", "cierran", "domingos"],
  "response": "Nuestro horario de atención es: {{dealer_hours}}. ¿En qué más puedo ayudarte?"
}

═══════════════════════════════════════════
TAREA 4: EVALUAR CALIDAD DEL MODELO
═══════════════════════════════════════════

Métricas a calcular:
- Tasa de fallback: % de mensajes con isFallback=true
- Tasa de resolución: % de conversaciones que terminaron con resultado positivo
- Confidence promedio: Promedio de confidenceScore
- Tasa de transferencia: % que requirieron agente humano
- Problemas legales: ¿Alguna respuesta debería haber sido bloqueada por auditoría?
- Alucinaciones: ¿Alguna respuesta mencionó datos que no estaban en el inventario?

═══════════════════════════════════════════
FORMATO DE RESPUESTA
═══════════════════════════════════════════

{
  "analysis_date": "2026-02-15",
  "model_version": "okla-chatbot-v2",
  "conversations_analyzed": 1247,

  "fine_tuning_candidates": [
    {
      "quality": "high",
      "reason": "Successful appointment scheduled",
      "messages": [
        {"role": "system", "content": "..."},
        {"role": "user", "content": "..."},
        {"role": "assistant", "content": "..."}
      ],
      "anonymized": true
    }
  ],
  "fine_tuning_count": 85,

  "fallback_analysis": {
    "total_fallbacks": 43,
    "groups": [
      {
        "theme": "Horarios de fin de semana",
        "count": 12,
        "sample_questions": ["¿Abren domingos?", "¿Trabajan sábados?"],
        "suggested_resolution": "quick_response",
        "proposed_response": "Nuestro horario es {{dealer_hours}}."
      }
    ]
  },

  "suggested_quick_responses": [
    {
      "name": "Horarios",
      "keywords": ["horario", "abierto", "domingos"],
      "response": "Nuestro horario: {{dealer_hours}}. ¿En qué más puedo ayudarte? 😊",
      "estimated_weekly_matches": 12
    }
  ],

  "quality_metrics": {
    "fallback_rate": 0.034,
    "resolution_rate": 0.89,
    "avg_confidence": 0.91,
    "transfer_rate": 0.12,
    "legal_issues_found": 0,
    "hallucinations_found": 1,
    "hallucination_details": ["Mencionó color 'Rojo' para RAV4 que solo está en Blanco y Gris"]
  },

  "recommendations": [
    "Agregar Quick Response para horarios (12 fallbacks/semana)",
    "Re-entrenar: modelo confunde 'motor' (moto) con motor del vehículo en 3 casos",
    "Revisar inventario sync: 1 alucinación de color detectada"
  ]
}
```

---

## 6. Notas de Implementación (.NET 8)

### Reemplazar `AutoLearningService`:

```csharp
// ELIMINAR: AutoLearningService con clustering por palabras
// CREAR: LlmAnalysisService que use el prompt de análisis

public class LlmAnalysisService : IAutoLearningService
{
    private readonly ILlmService _llmService;
    private readonly IChatSessionRepository _sessionRepo;
    private readonly IChatMessageRepository _messageRepo;

    public async Task<AutoLearningAnalysisResult> AnalyzeAndSuggestAsync(
        Guid configurationId, CancellationToken ct)
    {
        // 1. Obtener conversaciones de los últimos 7 días
        var conversations = await _sessionRepo.GetCompletedSessionsAsync(
            configurationId, DateTime.UtcNow.AddDays(-7), ct);

        // 2. Obtener fallbacks
        var fallbacks = await _messageRepo.GetFallbacksAsync(
            configurationId, DateTime.UtcNow.AddDays(-7), ct);

        // 3. Anonimizar PII antes de enviar al LLM
        var anonymized = AnonymizePII(conversations);

        // 4. Enviar al LLM con prompt de análisis
        var analysis = await _llmService.AnalyzeConversationsAsync(
            anonymized, fallbacks, ct);

        // 5. Guardar candidatos de fine-tuning en tabla training_candidates
        foreach (var candidate in analysis.FineTuningCandidates)
        {
            await _trainingCandidateRepo.AddAsync(candidate, ct);
        }

        // 6. Crear Quick Responses sugeridas (pendientes de aprobación admin)
        foreach (var qr in analysis.SuggestedQuickResponses)
        {
            await _quickResponseRepo.AddSuggestionAsync(configurationId, qr, ct);
        }

        return analysis;
    }
}
```

### Anonimización de PII:

```csharp
private string AnonymizePII(string text)
{
    // Cédulas: XXX-XXXXXXX-X → ***-*******-*
    text = Regex.Replace(text, @"\d{3}-\d{7}-\d", "***-*******-*");

    // Teléfonos: +1-809-XXX-XXXX → +1-809-***-****
    text = Regex.Replace(text, @"\+?1?-?(\d{3})-(\d{3})-(\d{4})", "+1-$1-***-****");

    // Tarjetas: 16 dígitos → ****-****-****-XXXX
    text = Regex.Replace(text, @"\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?(\d{4})", "****-****-****-$1");

    // Emails: user@domain.com → u***@domain.com
    text = Regex.Replace(text, @"(\w)\w+@(\w+\.\w+)", "$1***@$2");

    return text;
}
```
