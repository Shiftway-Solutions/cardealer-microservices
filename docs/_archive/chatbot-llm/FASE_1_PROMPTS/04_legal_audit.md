# 📐 Prompt 04 — Auditoría Legal (Pre-envío)

> **Fase:** 1 — Diseño de Prompts  
> **Última actualización:** Febrero 15, 2026

---

## 1. Nombre y Rol

**Legal Audit Prompt** — Audita cada respuesta del LLM ANTES de enviarla al usuario. Verifica cumplimiento legal con leyes dominicanas, precisión de datos vs inventario, ausencia de compromisos vinculantes y seguridad de datos sensibles. Se ejecuta como segunda llamada al LLM (chain-of-thought) o mediante un modelo auditor más pequeño.

---

## 2. Trigger

- **Cuándo se ejecuta:** Después de que el LLM genera una respuesta y ANTES de enviarla al usuario.
- **Qué lo activa:** Automáticamente en cada respuesta. Se puede desactivar vía `LlmSettings.EnableAudit`.

---

## 3. ⚠️ DIRECTIVA DE REEMPLAZO

> Este prompt es **NUEVO** — no existe equivalente en el ChatbotService actual basado en Dialogflow.
> Dialogflow NO tenía auditoría de respuestas. El nuevo sistema LLM DEBE implementar esta capa de seguridad legal que no existía antes.

---

## 4. Variables Dinámicas Requeridas

| Variable                   | Fuente                   | Tipo   | Ejemplo                      |
| -------------------------- | ------------------------ | ------ | ---------------------------- |
| `{{proposed_response}}`    | Output del LLM principal | string | Texto de respuesta generado  |
| `{{user_message}}`         | Mensaje del usuario      | string | "¿Cuánto cuesta el RAV4?"    |
| `{{inventory_context}}`    | Vehículos mencionados    | JSON   | Datos reales del inventario  |
| `{{conversation_history}}` | Últimos N mensajes       | JSON   | Historial de la conversación |
| `{{dealer_name}}`          | ChatbotConfiguration     | string | "Auto Toyota Dominicana"     |

---

## 5. Texto Completo del Prompt

```
═══════════════════════════════════════════
AUDITORÍA LEGAL DE RESPUESTA — CHATBOT OKLA
═══════════════════════════════════════════

Eres un auditor legal especializado en leyes dominicanas aplicadas a comercio electrónico y venta de vehículos. Tu trabajo es verificar que la respuesta del chatbot NO viole ninguna ley y sea segura para enviar.

RESPUESTA A AUDITAR:
"""
{{proposed_response}}
"""

MENSAJE ORIGINAL DEL USUARIO:
"""
{{user_message}}
"""

DATOS REALES DEL INVENTARIO (solo estos son válidos):
{{inventory_context}}

═══════════════════════════════════════════
CHECKLIST DE AUDITORÍA (verificar TODOS)
═══════════════════════════════════════════

### CHECK 1 — Ley 358-05 (Protección al Consumidor)
- [ ] ¿Menciona precios? → ¿Incluye disclaimer "sujeto a confirmación"?
- [ ] ¿Dice "precio final", "garantizado", "oferta especial"? → BLOQUEAR
- [ ] ¿Promete garantías específicas? → ¿Están documentadas formalmente?
- [ ] ¿Los precios están en DOP? (puede incluir USD como referencia)
- [ ] ¿Aclara que no incluye traspaso/impuestos/primera placa?

### CHECK 2 — Ley 172-13 (Protección de Datos)
- [ ] ¿Solicita datos personales? → ¿Pidió consentimiento explícito primero?
- [ ] ¿Repite datos sensibles del usuario (cédula, tarjeta, dirección completa)?
- [ ] ¿Informa para qué se usarán los datos?
- [ ] ¿Menciona la Ley 172-13 al pedir datos?

### CHECK 3 — Código Civil (Obligaciones contractuales)
- [ ] ¿Hace promesas vinculantes? (precio fijo, disponibilidad garantizada, plazos de entrega)
- [ ] ¿Dice "te garantizamos", "es seguro que", "sin duda"?
- [ ] ¿Ofrece condiciones de financiamiento específicas (tasa, plazo, cuota)?

### CHECK 4 — DGII (Normas fiscales)
- [ ] ¿Cotiza "todo incluido"? → BLOQUEAR (requiere validación humana)
- [ ] ¿Menciona ITBIS o impuestos específicos sin disclaimer?

### CHECK 5 — Precisión de datos
- [ ] ¿Menciona un vehículo? → ¿Los datos (precio, año, specs) coinciden con el inventario?
- [ ] ¿Inventa especificaciones que no están en el inventario?
- [ ] ¿Dice que un vehículo está disponible cuando no está en el inventario?

### CHECK 6 — Datos sensibles (PII)
- [ ] ¿La respuesta contiene cédulas (XXX-XXXXXXX-X)?
- [ ] ¿La respuesta contiene números de tarjeta (16 dígitos)?
- [ ] ¿La respuesta repite direcciones completas del usuario?
- [ ] ¿La respuesta repite datos de pago?

### CHECK 7 — Tono y profesionalismo
- [ ] ¿El tono es apropiado y profesional?
- [ ] ¿Contiene lenguaje ofensivo, discriminatorio o inapropiado?
- [ ] ¿Da consejos médicos, legales o financieros que requieran profesional?

═══════════════════════════════════════════
DECISIÓN
═══════════════════════════════════════════

Basado en tu auditoría, responde con EXACTAMENTE este JSON:

{
  "verdict": "APPROVED | NEEDS_REVISION | BLOCKED",
  "original_response": "[respuesta original sin modificar]",
  "revised_response": "[respuesta corregida si NEEDS_REVISION, null si APPROVED o BLOCKED]",
  "flags": ["lista de problemas encontrados"],
  "legal_references": ["leyes/artículos violados"],
  "pii_detected": false,
  "pii_details": [],
  "risk_level": "low | medium | high | critical",
  "audit_notes": "Explicación breve de la decisión"
}

REGLAS DE DECISIÓN:
- APPROVED: La respuesta cumple con TODOS los checks. Enviar tal cual.
- NEEDS_REVISION: La respuesta tiene problemas menores corregibles. Proporciona versión corregida en "revised_response".
- BLOCKED: La respuesta tiene problemas GRAVES (compromiso vinculante, datos sensibles expuestos, info inventada). NO enviar bajo ninguna circunstancia.

PRIORIDADES:
1. SEGURIDAD LEGAL > Experiencia de usuario (mejor ser conservador que arriesgar un problema legal)
2. Si tienes DUDA → marcar como NEEDS_REVISION y agregar disclaimers
3. Respuestas con precios SIEMPRE requieren disclaimer (no es motivo de BLOCKED, pero sí de NEEDS_REVISION si falta)
```

---

## 6. Ejemplo Real de Input/Output

### Input:

```
Respuesta a auditar: "¡El Toyota RAV4 2024 está a RD$2,850,000! Es un precio excelente y te garantizamos disponibilidad. Ven mañana y te lo separamos."

Mensaje del usuario: "¿Cuánto cuesta el RAV4?"

Inventario: [{"brand":"Toyota","model":"RAV4","year":2024,"price":2850000,"isAvailable":true}]
```

### Output:

```json
{
  "verdict": "NEEDS_REVISION",
  "original_response": "¡El Toyota RAV4 2024 está a RD$2,850,000! Es un precio excelente y te garantizamos disponibilidad. Ven mañana y te lo separamos.",
  "revised_response": "¡El Toyota RAV4 2024 tiene un precio de referencia de RD$2,850,000*! Es una excelente opción. 🚗\n\n*Precio de referencia sujeto a confirmación. No incluye traspaso ni impuestos. Consulta con nuestro equipo para una cotización oficial y disponibilidad actual.\n\n¿Te gustaría agendar una visita para verlo?",
  "flags": [
    "price_without_disclaimer",
    "availability_guarantee",
    "binding_commitment_separar"
  ],
  "legal_references": [
    "Ley 358-05 Art. 83-84: Afirmación de precio sin disclaimer puede ser vinculante",
    "Ley 358-05 Art. 33: Falta clarificación sobre impuestos no incluidos",
    "Código Civil Art. 1101: 'Te lo separamos' puede constituir compromiso contractual"
  ],
  "pii_detected": false,
  "pii_details": [],
  "risk_level": "medium",
  "audit_notes": "Se removieron 3 problemas: (1) precio sin disclaimer, (2) garantía de disponibilidad, (3) compromiso de separar. Se reemplazó con versión conservadora con disclaimers apropiados."
}
```

---

## 7. Notas de Implementación (.NET 8)

### Interfaz del servicio de auditoría:

```csharp
// En ILlmService.cs — Método de auditoría:
Task<AuditResult> AuditResponseAsync(
    string proposedResponse,
    string userMessage,
    string? inventoryContext,
    CancellationToken ct = default);

// DTO:
public record AuditResult(
    string Verdict,           // "APPROVED", "NEEDS_REVISION", "BLOCKED"
    string OriginalResponse,
    string? RevisedResponse,
    List<string> Flags,
    List<string> LegalReferences,
    bool PiiDetected,
    List<string> PiiDetails,
    string RiskLevel,         // "low", "medium", "high", "critical"
    string AuditNotes
);
```

### Integración en el pipeline de mensajes:

```csharp
// En SendMessageCommandHandler — DESPUÉS del LLM, ANTES de enviar:

var llmResponse = await _llmService.GenerateResponseAsync(request, ct);

// Auditar respuesta (si está habilitado)
if (_settings.EnableAudit)
{
    var auditResult = await _llmService.AuditResponseAsync(
        llmResponse.Response,
        request.Message,
        inventoryContext,
        ct);

    switch (auditResult.Verdict)
    {
        case "APPROVED":
            botResponse = llmResponse.Response;
            break;

        case "NEEDS_REVISION":
            botResponse = auditResult.RevisedResponse ?? llmResponse.Response;
            _logger.LogWarning("Audit revised response. Flags: {Flags}",
                string.Join(", ", auditResult.Flags));
            break;

        case "BLOCKED":
            botResponse = "Disculpa, no puedo darte esa información por este canal. " +
                          $"Te invito a comunicarte con nuestro equipo al {config.DealerPhone} " +
                          "para una asistencia personalizada.";
            _logger.LogError("Audit BLOCKED response. Flags: {Flags}, Legal: {Legal}",
                string.Join(", ", auditResult.Flags),
                string.Join(", ", auditResult.LegalReferences));
            break;
    }

    // Registrar auditoría en AuditService
    await _auditClient.LogActionAsync(new AuditLogRequest
    {
        Action = $"LLM_AUDIT_{auditResult.Verdict}",
        EntityType = "ChatMessage",
        Details = JsonSerializer.Serialize(auditResult)
    });
}
```

### Modelo auditor (puede ser más pequeño):

```json
// En appsettings.json:
{
  "LlmService": {
    "EnableAudit": true,
    "AuditModelId": "meta-llama/Llama-3-8B-Instruct",
    "AuditTimeoutSeconds": 5,
    "AuditMaxTokens": 512
  }
}
```

### Optimización de latencia:

```csharp
// La auditoría agrega latencia (~1-2s). Opciones:
// 1. Auditar con modelo más pequeño (8B vs 70B) — recomendado
// 2. Cache de auditorías similares en Redis (hash de response → resultado)
// 3. Auditoría async post-envío (riesgo: usuario ya vio respuesta problemática)
// 4. Solo auditar respuestas con keywords de riesgo (precio, garantía, etc.)

// Recomendación: Opción 1 + 4 combinadas
private bool RequiresAudit(string response)
{
    var riskKeywords = new[] {
        "precio", "costo", "vale", "garantía", "garantizamos",
        "seguro", "disponible", "separar", "reservar", "todo incluido",
        "tasa", "cuota", "financiamiento", "crédito"
    };
    return riskKeywords.Any(k => response.Contains(k, StringComparison.OrdinalIgnoreCase));
}
```
